namespace HandlingLargeBlobs.Infrastructure;

/// <summary>
/// Settings for Amazon S3 storage.
/// </summary>
public sealed class S3Settings
{
    /// <summary>
    /// Gets name of the S3 bucket.
    /// </summary>
    required public string BucketName { get; init; }

    /// <summary>
    /// Gets AWS region where the S3 bucket resides.
    /// </summary>
    required public string Region { get; init; }
}
