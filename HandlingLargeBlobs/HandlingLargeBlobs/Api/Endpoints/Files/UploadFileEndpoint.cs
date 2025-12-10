using System;
using System.Threading.Tasks;
using System.Linq;
using Amazon.S3;
using Amazon.S3.Model;
using HandlingLargeBlobs.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace HandlingLargeBlobs.Api.Endpoints.Files;

public static class UploadFileEndpoint
{
    public static void MapFileEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("files/presigned-url", GetPresignedUrl)
           .WithName("GetPresignedUrl")
           .WithTags("Files");

        app.MapPost("files/start-multipart", StartMultiPart)
           .WithName("StartMultipartUpload")
           .WithTags("Files");

        app.MapPost("files/start-parallel-multipart", StartParallelMultipart)
           .WithName("StartParallelMultipartUpload")
           .WithTags("Files");

        app.MapPost("images/{key}/presigned-part", GetPresignedPartUrl)
           .WithName("GetPresignedPartUrl")
           .WithTags("Files");

        app.MapPost("images/{key}/complete-multipart", CompleteMultipart)
           .WithName("CompleteMultipartUpload")
           .WithTags("Files");

        app.MapGet("images/{key}/{uploadId}/parts", ListParts)
           .WithName("ListMultipartUploadParts")
           .WithTags("Files");
    }

    private static IResult GetPresignedUrl(
        string fileName,
        string contentType,
        IAmazonS3 s3Client,
        IOptions<S3Settings> s3Settings)
    {
        try
        {
            var key = Guid.NewGuid();
            var request = new GetPreSignedUrlRequest
            {
                BucketName = s3Settings.Value.BucketName,
                Key = $"images/{key}",
                Verb = HttpVerb.PUT,
                Expires = DateTime.UtcNow.AddMinutes(15),
                ContentType = contentType,
                Metadata =
                {
                    ["file-name"] = fileName,
                },
            };

            string preSignedUrl = s3Client.GetPreSignedURL(request);

            return Results.Ok(new { key, url = preSignedUrl });
        }
        catch (AmazonS3Exception ex)
        {
            return Results.BadRequest($"S3 error generating pre-signed URL: {ex.Message}");
        }
    }

    private static async Task<IResult> StartMultiPart(
        string fileName,
        string contentType,
        IAmazonS3 s3Client,
        IOptions<S3Settings> s3Settings)
    {
        try
        {
            var key = Guid.NewGuid();
            var request = new InitiateMultipartUploadRequest
            {
                BucketName = s3Settings.Value.BucketName,
                Key = $"images/{key}",
                ContentType = contentType,
                Metadata =
                {
                    ["file-name"] = fileName
                }
            };

            var response = await s3Client.InitiateMultipartUploadAsync(request);

            return Results.Ok(new { key, uploadId = response.UploadId });
        }
        catch (AmazonS3Exception ex)
        {
            return Results.BadRequest($"S3 error starting multipart upload: {ex.Message}");
        }
    }

    private static IResult GetPresignedPartUrl(
        string key,
        string uploadId,
        int partNumber,
        IAmazonS3 s3Client,
        IOptions<S3Settings> s3Settings)
    {
        try
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = s3Settings.Value.BucketName,
                Key = $"images/{key}",
                Verb = HttpVerb.PUT,
                Expires = DateTime.UtcNow.AddMinutes(15),
                UploadId = uploadId,
                PartNumber = partNumber
            };

            string preSignedUrl = s3Client.GetPreSignedURL(request);

            return Results.Ok(new { key, url = preSignedUrl });
        }
        catch (AmazonS3Exception ex)
        {
            return Results.BadRequest($"S3 error generating pre-signed URL for part: {ex.Message}");
        }
    }

    private static async Task<IResult> CompleteMultipart(
        string key,
        CompleteMultipartUpload complete,
        IAmazonS3 s3Client,
        IOptions<S3Settings> s3Settings)
    {
        try
        {
            var request = new CompleteMultipartUploadRequest
            {
                BucketName = s3Settings.Value.BucketName,
                Key = $"images/{key}",
                UploadId = complete.UploadId,
                PartETags = complete.Parts.Select(p => new PartETag(p.PartNumber, p.ETag)).ToList()
            };

            var response = await s3Client.CompleteMultipartUploadAsync(request);

            return Results.Ok(new { key, location = response.Location });
        }
        catch (AmazonS3Exception ex)
        {
            return Results.BadRequest($"S3 error completing multipart upload: {ex.Message}");
        }
    }

    private static async Task<IResult> ListParts(
        string key,
        string uploadId,
        IAmazonS3 s3Client,
        IOptions<S3Settings> s3Settings)
    {
        try
        {
            var parts = new List<PartDetail>();
            ListPartsRequest request = new ListPartsRequest
            {
                BucketName = s3Settings.Value.BucketName,
                Key = $"images/{key}",
                UploadId = uploadId,
            };

            ListPartsResponse response;
            do
            {
                response = await s3Client.ListPartsAsync(request).ConfigureAwait(false);
                parts.AddRange(response.Parts);
                request.PartNumberMarker = response.NextPartNumberMarker.ToString();
            }
            while (response.IsTruncated);

            // Return a lightweight list of part numbers that are already done
            return Results.Ok(parts.Select(p => new { p.PartNumber, p.ETag }));
        }
        catch (AmazonS3Exception ex)
        {
            return Results.BadRequest($"S3 error listing parts: {ex.Message}");
        }
    }

    private static async Task<IResult> StartParallelMultipart(
        string fileName,
        string contentType,
        long fileSize,
        IAmazonS3 s3Client,
        IOptions<S3Settings> s3Settings)
    {
        try
        {
            const long CHUNK_SIZE = 5 * 1024 * 1024; // 5MB
            int totalParts = (int)Math.Ceiling((double)fileSize / CHUNK_SIZE);
            var key = Guid.NewGuid();

            // 1. Initiate the upload (Network Call - Must wait)
            var initRequest = new InitiateMultipartUploadRequest
            {
                BucketName = s3Settings.Value.BucketName,
                Key = $"images/{key}",
                ContentType = contentType,
                Metadata = { ["file-name"] = fileName },
            };
            var initResponse = await s3Client.InitiateMultipartUploadAsync(initRequest).ConfigureAwait(false);

            // 2. Generate URLs in Parallel (CPU Bound)
            // We use a fixed array instead of a List to avoid locking/contention overhead
            var presignedUrlResults = new object[totalParts];

            // This runs on multiple threads simultaneously
            Parallel.For(0, totalParts, i =>
            {
                int partNumber = i + 1;

                var urlRequest = new GetPreSignedUrlRequest
                {
                    BucketName = s3Settings.Value.BucketName,
                    Key = $"images/{key}",
                    Verb = HttpVerb.PUT,
                    // Critical: Ensure expiry covers the whole upload duration!
                    Expires = DateTime.UtcNow.AddHours(12),
                    UploadId = initResponse.UploadId,
                    PartNumber = partNumber
                };

                // Generating the string is CPU work; doing it in parallel is faster
                string signedUrl = s3Client.GetPreSignedURL(urlRequest);

                // Store directly in the array index (Thread-safe because indices are unique)
                presignedUrlResults[i] = new { partNumber, url = signedUrl };
            });

            return Results.Ok(new {
                key,
                uploadId = initResponse.UploadId,
                urls = presignedUrlResults
            });
        }
        catch (AmazonS3Exception ex)
        {
            return Results.BadRequest($"S3 error: {ex.Message}");
        }
    }
}

public class CompleteMultipartUpload
{
    [Required]
    public string Key { get; set; }

    [Required]
    public string UploadId { get; set; }

    [Required]
    public List<PartETagInfo> Parts { get; set; }
}

public class PartETagInfo
{
    [Required]
    public int PartNumber { get; set; }

    [Required]
    public string ETag { get; set; }
}
