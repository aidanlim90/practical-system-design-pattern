using LaunchDarkly.OpenFeature.ServerProvider;
using LaunchDarkly.Sdk.Server;
using OpenFeature;
using OpenFeature.Model;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// var ldConfig = Configuration.Builder("YOUR_LAUNCHDARKLY_SDK_KEY").Build();
// var ldProvider = new Provider(ldConfig);
// await Api.Instance.SetProviderAsync(ldProvider);
builder.Services.AddOpenFeature(featureBuilder =>
{
    // Register the LaunchDarkly Provider
    featureBuilder.AddProvider(sp =>
    {
        var ldConfig = Configuration.Builder("sdk-ecc90dd1-a2e6-4857-a50f-05a6150b60a7").Build();

        return new Provider(ldConfig);
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing",
    "Bracing",
    "Chilly",
    "Cool",
    "Mild",
    "Warm",
    "Balmy",
    "Hot",
    "Sweltering",
    "Scorching",
};

app.MapGet(
        "/weatherforecast",
        async (IFeatureClient featureClient) =>
        {
            var context = EvaluationContext.Builder().Set("targetingKey", "Charles123").Build();
            var isEnabled = await featureClient.GetBooleanValueAsync(
                "sample-feature",
                false,
                context
            );
            if (!isEnabled)
            {
                return Array.Empty<WeatherForecast>();
            }
            var forecast = Enumerable
                .Range(1, 5)
                .Select(index => new WeatherForecast(
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
                .ToArray();
            return forecast;
        }
    )
    .WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
