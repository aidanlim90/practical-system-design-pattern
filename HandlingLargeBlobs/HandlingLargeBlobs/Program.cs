using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using HandlingLargeBlobs.Api.Endpoints.Files;
using HandlingLargeBlobs.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

var s3Section = builder.Configuration.GetSection("S3Settings");
builder.Services
    .Configure<S3Settings>(s3Section)
    .AddEndpointsApiExplorer()
    .AddSwaggerGen()
    .AddCors()
    .AddInfrastructure();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
}
app.MapFileEndpoints();
app.Run();
