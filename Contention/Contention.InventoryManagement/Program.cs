
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Contention.InventoryManagement
{
    public class Program
    {
        public class RedisSettings
        {
            public const string SectionName = "Redis";
            public string Host { get; set; }
            public int Port { get; set; }
            public bool UseSsl { get; set; }
            public string AuthToken { get; set; }
        }

        private static ConfigurationOptions ConfigureRedis(RedisSettings settings, string host, int port)
        {
            var cfg = new ConfigurationOptions
            {
                AbortOnConnectFail = false,
                EndPoints = { { host, port } },
                Ssl = settings.UseSsl,
                Password = settings.AuthToken,
                KeepAlive = 180,
            };

            return cfg;
        }
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddAuthorization();

            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();
            var redisSection = builder.Configuration.GetSection(RedisSettings.SectionName);
            builder.Services.Configure<RedisSettings>(redisSection);
            var redisSettings = redisSection.Get<RedisSettings>()!;
            var redisConfig = ConfigureRedis(redisSettings, redisSettings.Host, redisSettings.Port);
            builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConfig));

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseAuthorization();

            app.MapGet("/purchase", async (HttpContext httpContext, IConnectionMultiplexer connection, int buyers = 1500,int qtyPerBuyer = 1) =>
            {
                var db = connection.GetDatabase();
                var key = $"product:123:stock";

                // Lua script to check and decrement atomically
                var script = @"
                    local stock = redis.call('GET', KEYS[1])
                    if not stock then return -1 end
        
                    stock = tonumber(stock)
                    local qty = tonumber(ARGV[1])
        
                    if stock >= qty then
                        redis.call('DECRBY', KEYS[1], qty)
                        return stock - qty
                    end
                    return -2";

                int success = 0;
                int failed = 0;

                await Parallel.ForEachAsync(
                    Enumerable.Range(0, buyers),
                    new ParallelOptions { MaxDegreeOfParallelism = buyers },
                    async (_, _) =>
                    {
                        var result = (int)await db.ScriptEvaluateAsync(
                            script,
                            new RedisKey[] { key },
                            new RedisValue[] { qtyPerBuyer }
                        );

                        if (result >= 0)
                            Interlocked.Increment(ref success);
                        else
                            Interlocked.Increment(ref failed);
                    });

                //            var result = (int)await db.ScriptEvaluateAsync(
                //                   script,
                //new RedisKey[] { key },     // KEYS[1]
                //new RedisValue[] { 1 }
                //            );
                //            return result;

                var finalStock = (int)await db.StringGetAsync(key);

                return Results.Ok(new
                {
                    buyers,
                    success,
                    failed,
                    finalStock
                });
            })
            .WithName("GetWeatherForecast");


            app.Run();

        }
    }
}
