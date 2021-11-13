using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PMMarketDataService.DataProvider.CacheService.Implementations;
using PMMarketDataService.DataProvider.HistoricalDataService.Implementations;
using PMMarketDataServiceAPI.Models;
using PseudoMarkets.Infra.ConfigServer.Client.Interfaces;

namespace PMMarketDataServiceAPI.HealthCheck
{
    public class MarketDataServiceHealthCheck : IHealthCheck
    {
        private readonly PseudoMarketsDbContext _pseudoMarketsDb;
        private readonly AerospikeDataManager _aerospikeDataManager;
        private readonly MongoDataManager _mongoDataManager;
        private readonly IConfigServer _configServer;

        public MarketDataServiceHealthCheck(PseudoMarketsDbContext dbContext, AerospikeDataManager aeroDataManager,
            MongoDataManager mongoDataManager, IConfigServer configServer)
        {
            _pseudoMarketsDb = dbContext;
            _aerospikeDataManager = aeroDataManager;
            _mongoDataManager = mongoDataManager;
            _configServer = configServer;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
        {
            var statusDictionary = new Dictionary<string, object>();

            try
            {
                statusDictionary.Add("Aerospike", _aerospikeDataManager.IsConnected() ? "Connected" : "Disconnected");

                statusDictionary.Add("Mongo", _mongoDataManager.IsConnected() ? "Connected" : "Disconnected");

                statusDictionary.Add("Config Server", _configServer.IsConnected() ? "Connected" : "Disconnected");

                statusDictionary.Add("Pseudo Markets DB (SQL/RDS)", await _pseudoMarketsDb.Database.CanConnectAsync(cancellationToken) ? "Connected" : "Disconnected" );

                HealthStatus overallStatus;

                if (statusDictionary.Values.Select(x => x.ToString()).Any(y => y == "Disconnected"))
                {
                    overallStatus = HealthStatus.Degraded;
                }
                else
                {
                    overallStatus = HealthStatus.Healthy;
                }

                return new HealthCheckResult(overallStatus, "Market Data Service Health Check Passed", null,
                    statusDictionary);
            }
            catch (Exception e)
            {
                return new HealthCheckResult(HealthStatus.Unhealthy, "Market Data Service Health Check Failed", e, statusDictionary);
            }

        }
    }
}
