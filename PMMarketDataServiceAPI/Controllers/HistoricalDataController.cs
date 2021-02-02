using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PMCommonEntities.Models.HistoricalData;
using PMCommonEntities.Models.PseudoXchange;
using PMMarketDataService.DataProvider.Lib.Implementation;
using PMMarketDataServiceAPI.Models;
using PMMarketDataServiceAPI.MongoDb.Implementation;
using PMUnifiedAPI.Models;
using Serilog;

namespace PMMarketDataServiceAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class HistoricalDataController : ControllerBase
    {
        private readonly MongoDataManager _mongoConnection;
        private readonly PseudoMarketsDbContext _pseudoMarketsDb;
        private readonly MarketDataProvider _marketDataProvider;
        public HistoricalDataController(MongoDataManager mongoConnectionManager,
            PseudoMarketsDbContext pseudoMarketsDbContext, MarketDataProvider marketDataProvider)
        {
            _mongoConnection = mongoConnectionManager;
            _pseudoMarketsDb = pseudoMarketsDbContext;
            _marketDataProvider = marketDataProvider;
        }

        // GET: api/HistoricalData/LoadHistoricalData
        [HttpGet]
        [Route("LoadHistoricalData")]
        public async Task<ActionResult> LoadHistoricalData()
        {
            try
            {
                var positions = _pseudoMarketsDb.Positions.Select(x => x.Symbol).Distinct().ToList();
                foreach (string symbol in positions)
                {
                    var priceData = await _marketDataProvider.GetTwelveDataRealTimePrice(symbol);
                    HistoricalStockData historicalData = new HistoricalStockData()
                    {
                        ClosingPrice = Convert.ToDouble(priceData?.Price),
                        Symbol = symbol.ToUpper(),
                        Date = DateTime.Today
                    };

                    _mongoConnection.SaveHistoricalStockData(historicalData);

                    // Add a delay to stay within Twelve Data API limit
                    Thread.Sleep(5000);
                }

                return Ok("LOAD OK");
            }
            catch (Exception e)
            {
                Log.Error(e, $"{nameof(LoadHistoricalData)}");
                return StatusCode(500);
            }
        }

        // GET: api/HistoricalData/GetHistoricalData/{symbol}/{date}
        [HttpGet]
        [Route("GetHistoricalData/{symbol}/{date}")]
        public HistoricalStockData GetHistoricalData(string symbol, string date)
        {
            try
            {
                var historicalData = _mongoConnection.GetHistoricalStockData(symbol, date);

                return historicalData;

            }
            catch (Exception e)
            {
                Log.Error(e, $"{nameof(GetHistoricalData)}");
                return new HistoricalStockData();
            }
        }
    }
}
