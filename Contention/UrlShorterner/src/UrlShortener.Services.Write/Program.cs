using UrlShortener.Services.Write.Api;
using UrlShortener.Services.Write.Application;
using UrlShortener.Services.Write.Domain;
using UrlShortener.Services.Write.Infrastructure;
using UrlShortener.Services.Write.Infrastructure.Persistence;
using UrlShortener.Services.Write.Api.Endpoints.Urls;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Services
    .AddDomain()
    .AddApplication()
    .AddInfrastructure(configuration)
    .AddApi();

var app = builder.Build();
app.UseExceptionHandler();
app.ApplyMigrations();
app.MapShortUrlEndpoints();
app.Run();
