using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.IISIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using PMMarketDataService.DataProvider.CacheService.Implementations;
using PMMarketDataService.DataProvider.HistoricalDataService.Implementations;
using PMMarketDataService.DataProvider.Lib.Implementation;
using PMMarketDataService.DataProvider.Lib.Interfaces;
using PMMarketDataServiceAPI.Models;

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
            // Inject context for Relational Data Store and Historical Data store systems
            services.AddDbContext<PseudoMarketsDbContext>(options => options.UseSqlServer(Configuration.GetConnectionString("SqlServer")));
            services.AddSingleton<MongoDataManager>(
                new MongoDataManager(Configuration.GetConnectionString("MongoDb")));

            // Inject context for Real Time Data store system
            services.AddSingleton<AerospikeDataManager>(new AerospikeDataManager(
                Configuration.GetValue<string>("AerospikeConfig:Hostname"),
                Configuration.GetValue<int>("AerospikeConfig:Port"), Configuration.GetValue<int>("AerospikeConfig:CacheTtl")));

            // Inject Market Data Provider library
            services.AddSingleton<MarketDataProvider>(new MarketDataProvider(new HttpClient(),   
                Configuration.GetValue<string>("DataServiceConfig:AlphaVantageApiKey"),
                Configuration.GetValue<string>("DataServiceConfig:TwelveDataApiKey"),
                Configuration.GetValue<string>("DataServiceConfig:IexApiKey")));

            // Add basic auth
            services.AddAuthentication(IISDefaults.AuthenticationScheme);

            // Configure all other services
            services.Configure<DataServiceConfig>(Configuration.GetSection("DataServiceConfig"));
            services.AddControllers();
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
            // Add header forwarding when running behind a reverse proxy on Linux hosts
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                app.UseForwardedHeaders(new ForwardedHeadersOptions
                {
                    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
                });
            }
            
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
            });
        }
    }
}
