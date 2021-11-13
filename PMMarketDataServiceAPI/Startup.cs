using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.IISIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using PMMarketDataService.DataProvider.CacheService.Implementations;
using PMMarketDataService.DataProvider.HistoricalDataService.Implementations;
using PMMarketDataService.DataProvider.Lib.Implementation;
using PMMarketDataServiceAPI.HealthCheck;
using PMMarketDataServiceAPI.Models;
using PseudoMarkets.Infra.ConfigServer.Client.Extensions;
using PseudoMarkets.Infra.ConfigServer.Client.Implementations;
using PseudoMarkets.Infra.ConfigServer.Client.Models;

namespace PMMarketDataServiceAPI
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Inject Config Server
            var configServer = new PseudoMarketsConfigServer(Configuration.GetValue<string>("AerospikeConfig:Hostname"),
                Configuration.GetValue<int>("AerospikeConfig:Port"));

            services.AddConfigServer(configServer);

            var configs = configServer.GetConfigs(Configuration.GetValue<string>("DataServiceConfig:ConfigServerAppName"), true);

            // Inject context for Relational Data Store and Historical Data store systems
            services.AddDbContext<PseudoMarketsDbContext>(options => options.UseSqlServer(configs[MarketDataServiceAppConfig.SqlConnectionString]));

            services.AddSingleton<MongoDataManager>(
                new MongoDataManager(configs[MarketDataServiceAppConfig.MongoDbConnectionString]));

            // Inject context for Real Time Data store system
            services.AddSingleton<AerospikeDataManager>(new AerospikeDataManager(
                configs[MarketDataServiceAppConfig.AerospikeHost],
                Convert.ToInt32(configs[MarketDataServiceAppConfig.AeroPort]), Convert.ToInt32(configs[MarketDataServiceAppConfig.AerospikeTtl])));

            // Inject Market Data Provider library
            services.AddSingleton<MarketDataProvider>(new MarketDataProvider(new HttpClient(),   
                configs[MarketDataServiceAppConfig.AlphaVantageKey],
                configs[MarketDataServiceAppConfig.TwelveDataKey],
                configs[MarketDataServiceAppConfig.IexKey]));

            // Add basic auth
            services.AddAuthentication(IISDefaults.AuthenticationScheme);

            // Configure all other services

            var config = new DataServiceConfig()
            {
                ServiceVersion = configs[MarketDataServiceAppConfig.Version]
            };

            services.AddSingleton<DataServiceConfig>(config);
            services.AddControllers();
            services.AddHealthChecks()
                .AddCheck<MarketDataServiceHealthCheck>("Market Data Service Health Check");


            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "Pseudo Markets MDS API",
                    Description = "Market Data Service for delivering real-time and cached stock and ETF quotes along with historical data",
                    Contact = new OpenApiContact
                    {
                        Name = "Shravan Jambukesan",
                        Email = "shravan@shravanj.com",
                        Url = new Uri("https://github.com/ShravanJ")
                    },
                    License = new OpenApiLicense
                    {
                        Name = "MIT License",
                        Url = new Uri("https://github.com/pseudomarkets/PMUnifiedAPI/blob/master/LICENSE.txt")
                    }
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Market Data Service API");
            });

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/health", new HealthCheckOptions()
                {
                    ResponseWriter = WriteResponse
                });
            });
        }

        private static Task WriteResponse(HttpContext context, HealthReport result)
        {
            context.Response.ContentType = "application/json; charset=utf-8";

            var options = new JsonWriterOptions
            {
                Indented = true
            };

            using (var stream = new MemoryStream())
            {
                using (var writer = new Utf8JsonWriter(stream, options))
                {
                    writer.WriteStartObject();
                    writer.WriteString("status", result.Status.ToString());
                    writer.WriteStartObject("results");
                    foreach (var entry in result.Entries)
                    {
                        writer.WriteStartObject(entry.Key);
                        writer.WriteString("status", entry.Value.Status.ToString());
                        writer.WriteString("description", entry.Value.Description);
                        writer.WriteStartObject("data");
                        foreach (var item in entry.Value.Data)
                        {
                            writer.WritePropertyName(item.Key);
                            JsonSerializer.Serialize(
                                writer, item.Value, item.Value?.GetType() ??
                                                    typeof(object));
                        }
                        writer.WriteEndObject();
                        writer.WriteEndObject();
                    }
                    writer.WriteEndObject();
                    writer.WriteEndObject();
                }

                var json = Encoding.UTF8.GetString(stream.ToArray());

                return context.Response.WriteAsync(json);
            }
        }
    }
}
