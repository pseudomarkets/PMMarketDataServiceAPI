using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PMCommonApiModels.ResponseModels;
using PMCommonEntities.Models.PseudoXchange;
using PMMarketDataService.DataProvider.CacheService.Implementations;
using PMMarketDataService.DataProvider.Lib.Implementation;

namespace PMMarketDataServiceAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class MarketDataController : ControllerBase
    {
        private readonly MarketDataProvider _marketDataProvider;
        private readonly AerospikeDataManager _aerospikeConnectionManager;
        private bool _isCacheDisabled = false;
        private List<string> _cacheDisabledSymbols = new List<string>();

        public MarketDataController(MarketDataProvider marketDataProvider, AerospikeDataManager aerospikeConnectionManager)
        {
            _marketDataProvider = marketDataProvider;
            _aerospikeConnectionManager = aerospikeConnectionManager;
            _isCacheDisabled = _aerospikeConnectionManager.GetGlobalCacheDisableStatus();
            _cacheDisabledSymbols = aerospikeConnectionManager.GetCacheDisabledSymbolsList().ToList();
        }

        // GET: api/MarketData/LatestPrice/{symbol}
        [HttpGet]
        [Route("LatestPrice/{symbol}")]
        public async Task<LatestPriceOutput> GetLatestPrice(string symbol)
        {
            symbol = symbol.ToUpper();
            LatestPriceOutput output = new LatestPriceOutput();

            if (_isCacheDisabled || _cacheDisabledSymbols.Contains(symbol))
            {
                var data = await FetchLatestPrice(symbol);

                output.source = data.Source;
                output.symbol = symbol;
                output.price = data.LatestPrice;
                output.timestamp = DateTime.Now;
            }
            else
            {
                var cachedPrice = _aerospikeConnectionManager.GetCachedPrice(symbol,
                    XchangeInMemNamespace.SetLatestPriceCache.Set,
                    XchangeInMemNamespace.SetLatestPriceCache.CachedPriceBin);

                if (cachedPrice > 0)
                {
                    output.symbol = symbol;
                    output.price = cachedPrice;
                    output.timestamp = DateTime.Now;
                    output.source = "Pseudo Markets Cached Latest Price";
                }
                else
                {
                    var data = await FetchLatestPrice(symbol);

                    _aerospikeConnectionManager.SetCachedPrice(symbol, data.LatestPrice,
                        XchangeInMemNamespace.SetLatestPriceCache.Set,
                        XchangeInMemNamespace.SetLatestPriceCache.CachedPriceBin);

                    output.source = data.Source;
                    output.symbol = symbol;
                    output.price = data.LatestPrice;
                    output.timestamp = DateTime.Now;
                }
            }

            return output;
        }

        private async Task<(double LatestPrice, string Source)> FetchLatestPrice(string symbol)
        {
            double latestPrice = 0;
            string source = string.Empty;

            var topsData = await _marketDataProvider.GetIexTopsData(symbol);

            // IEX TOPS data is only available intraday, so we fallback to Twelve Data during non-market hours
            if (topsData?.bidPrice > 0 && topsData.askPrice > 0)
            {
                latestPrice = (topsData.bidPrice + topsData.askPrice) / 2;
                source = "IEX TOPS";
            }
            else
            {
                var twelveDataPrice = await _marketDataProvider.GetTwelveDataRealTimePrice(symbol);
                latestPrice = twelveDataPrice.Price;
                source = "Twelve Data Real Time Price";
            }

            return (latestPrice, source);
        }

        // GET: api/MarketData/AggregatePrice/{symbol}
        [HttpGet]
        [Route("AggregatePrice/{symbol}")]
        public async Task<LatestPriceOutput> GetAggregatePrice(string symbol)
        {
            symbol = symbol.ToUpper();
            LatestPriceOutput output = new LatestPriceOutput();

            if (_isCacheDisabled || _cacheDisabledSymbols.Contains(symbol))
            {
                var aggregatePrice = await FetchAggregatePrice(symbol);

                output.price = aggregatePrice;
                output.source = "Pseudo Markets Aggregate Real Time Price";
                output.symbol = symbol;
                output.timestamp = DateTime.Now;
            }
            else
            {
                var cachedPrice = _aerospikeConnectionManager.GetCachedPrice(symbol,
                    XchangeInMemNamespace.SetAggregatePriceCache.Set,
                    XchangeInMemNamespace.SetAggregatePriceCache.CachedPriceBin);

                if (cachedPrice > 0)
                {
                    output.symbol = symbol;
                    output.price = cachedPrice;
                    output.timestamp = DateTime.Now;
                    output.source = "Pseudo Markets Cached Aggregate Price";
                }
                else
                {
                    var aggregatePrice = await FetchAggregatePrice(symbol);

                    _aerospikeConnectionManager.SetCachedPrice(symbol, aggregatePrice, XchangeInMemNamespace.SetAggregatePriceCache.Set, XchangeInMemNamespace.SetAggregatePriceCache.CachedPriceBin);

                    output.price = aggregatePrice;
                    output.source = "Pseudo Markets Aggregate Real Time Price";
                    output.symbol = symbol;
                    output.timestamp = DateTime.Now;
                }
            }

            return output;
        }

        private async Task<double> FetchAggregatePrice(string symbol)
        {

            var twelveDataPrice = await _marketDataProvider.GetTwelveDataRealTimePrice(symbol);
            var iexPrice = await _marketDataProvider.GetIexTopsData(symbol);
            var alphaVantagePrice = await _marketDataProvider.GetAlphaVantageGlobalQuote(symbol);

            double aggregatePrice = 0;

            if (iexPrice?.askPrice > 0 && iexPrice?.bidPrice > 0)
            {
                aggregatePrice = (twelveDataPrice.Price + ((iexPrice.askPrice + iexPrice.bidPrice) / 2) +
                                  Convert.ToDouble(alphaVantagePrice.GlobalQuote.price)) / 3;
            }
            else
            {
                aggregatePrice = (twelveDataPrice.Price + Convert.ToDouble(alphaVantagePrice.GlobalQuote.price)) / 2;
            }

            return aggregatePrice;
        }

        // GET: api/MarketData/DetailedQuote/{symbol}
        [HttpGet]
        [Route("DetailedQuote/{symbol}/{interval}")]
        public async Task<DetailedQuoteOutput> GetDetailedQuote(string symbol, string interval = "1min")
        {

            symbol = symbol.ToUpper();

            DetailedQuoteOutput detailedQuote;

            if (_isCacheDisabled || _cacheDisabledSymbols.Contains(symbol))
            {
                detailedQuote = await _marketDataProvider.GetTwelveDataDetailedQuote(symbol, interval);
            }
            else
            {
                var cachedQuote = _aerospikeConnectionManager.GetCachedDetailedQuote(symbol);

                if (!string.IsNullOrEmpty(cachedQuote?.symbol))
                {
                    detailedQuote = cachedQuote;
                    detailedQuote.source = "Pseudo Markets Cached Detailed Quote";
                }
                else
                {
                    detailedQuote = await _marketDataProvider.GetTwelveDataDetailedQuote(symbol, interval);

                    if (detailedQuote != null && !string.IsNullOrEmpty(detailedQuote?.symbol))
                    {
                        _aerospikeConnectionManager.SetCachedDetailedQuote(detailedQuote);
                        detailedQuote.source = "Twelve Data Time Series";
                    }
                }
            }

            return detailedQuote;
        }

        // GET: api/MarketData/Indices
        [HttpGet]
        [Route("Indices")]
        public async Task<ActionResult> GetIndices()
        {
            var indices = new IndicesOutput();

            if (_isCacheDisabled || _cacheDisabledSymbols.Contains("US_INDICES"))
            {
                indices = await _marketDataProvider.GetTwelveDataIndices();

                return Ok(JsonConvert.SerializeObject(indices));
            }

            var cachedIndices = _aerospikeConnectionManager.GetCachedIndices();
            if (cachedIndices.Any())
            {
                cachedIndices.TryGetValue("DOW", out var dow);
                cachedIndices.TryGetValue("S&P 500", out var sp500);
                cachedIndices.TryGetValue("NASDAQ Composite", out var nasdaq);

                List<StockIndex> indexValues = new List<StockIndex>();

                indexValues.Add(new StockIndex()
                {
                    name = "DOW",
                    points = dow
                });

                indexValues.Add(new StockIndex()
                {
                    name = "S&P 500",
                    points = sp500
                });

                indexValues.Add(new StockIndex()
                {
                    name = "NASDAQ Composite",
                    points = nasdaq
                });

                indices = new IndicesOutput()
                {
                    indices = indexValues,
                    source = "Pseudo Markets Cached Indices",
                    timestamp = DateTime.Now
                };

                return Ok(JsonConvert.SerializeObject(indices));
            }

            indices = await _marketDataProvider.GetTwelveDataIndices();

            if (indices?.indices != null && indices.indices.Any())
            {
                var dowPoints = indices.indices?.Find(x => x?.name == "DOW")?.points;
                var sp500Points = indices.indices?.Find(x => x?.name == "S&P 500")?.points;
                var nasdaqPoints = indices.indices?.Find(x => x?.name == "NASDAQ Composite")?.points;

                _aerospikeConnectionManager.SetCachedIndices(dowPoints ?? 0.0, sp500Points ?? 0.0, nasdaqPoints ?? 0.0);
            }

            return Ok(JsonConvert.SerializeObject(indices));
        }
    }
}
