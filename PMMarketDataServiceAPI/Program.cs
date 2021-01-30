using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace PMMarketDataServiceAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {

            Log.Logger = new LoggerConfiguration().MinimumLevel.Information().MinimumLevel
                .Override("Microsoft", LogEventLevel.Information).Enrich.FromLogContext().WriteTo
                .File("MarketDataServiceLog.txt", rollingInterval: RollingInterval.Day).CreateLogger();

            try
            {
                CreateHostBuilder(args).Build().Run();
                Log.Information("Market Data Service starting up...");
            }
            catch (Exception e)
            {
                Log.Fatal(e, $"{nameof(Main)}");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
