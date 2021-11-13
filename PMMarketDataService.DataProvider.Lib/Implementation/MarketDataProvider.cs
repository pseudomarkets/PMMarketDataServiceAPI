using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PMCommonApiModels.ResponseModels;
using PMMarketDataService.DataProvider.Lib.Interfaces;
using TwelveDataSharp;
using TwelveDataSharp.Interfaces;
using TwelveDataSharp.Library.ResponseModels;

namespace PMMarketDataService.DataProvider.Lib.Implementation
{
    public class MarketDataProvider : IMarketDataProvider
    {
        private readonly HttpClient _client;
        private string _alphaVantageApiKey = string.Empty;
        private string _twelveDataApiKey = string.Empty;
        private string _iexApiKey = string.Empty;
        private ITwelveDataClient _twelveDataClient;

        public MarketDataProvider(HttpClient client, string alphaVantageApiKey, string twelveDataApiKey, string iexApiKey)
        {
            _client = client;
            _alphaVantageApiKey = alphaVantageApiKey;
            _twelveDataApiKey = twelveDataApiKey;
            _iexApiKey = iexApiKey;
            _twelveDataClient = new TwelveDataClient(twelveDataApiKey, _client);
        }

        public async Task<AlphaVantage.AlphaVantageGlobalQuote> GetAlphaVantageGlobalQuote(string symbol)
        {
            string avEndpoint = "https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol=" + symbol + "&apikey=" +
                                _alphaVantageApiKey;
            var avResponse = await _client.GetAsync(avEndpoint);
            string avJsonResponse = await avResponse.Content.ReadAsStringAsync();
            var avQuote = JsonConvert.DeserializeObject<AlphaVantage.AlphaVantageGlobalQuote>(avJsonResponse);

            return avQuote;
        }

        public async Task<IexCloudTops> GetIexTopsData(string symbol)
        {
            string iexEndpoint =
                "https://cloud.iexapis.com/stable/tops?token=" + _iexApiKey + "&symbols=" + symbol;
            var response = await _client.GetAsync(iexEndpoint);
            string jsonResponse = await response.Content.ReadAsStringAsync();
            var topsList = JsonConvert.DeserializeObject<List<IexCloudTops>>(jsonResponse);
            IexCloudTops topsData = null;
            if (topsList.Count > 0)
            {
                topsData = topsList[0];
            }

            return topsData;
        }

        public async Task<DetailedQuoteOutput> GetTwelveDataDetailedQuote(string symbol, string interval)
        {
            var detailedQuote = await _twelveDataClient.GetQuoteAsync(symbol, interval);
            DetailedQuoteOutput output = new DetailedQuoteOutput()
            {
                name = detailedQuote.Name,
                symbol = symbol,
                open = detailedQuote.Open,
                high = detailedQuote.High,
                low = detailedQuote.Low,
                close = detailedQuote.Close,
                volume = detailedQuote.Volume,
                previousClose = detailedQuote.PreviousClose,
                change = detailedQuote.Change,
                changePercentage = detailedQuote.PercentChange,
                timestamp = DateTime.Now
            };

            return output;
        }

        public async Task<IndicesOutput> GetTwelveDataIndices()
        {
            string tdEndpoint = "https://api.twelvedata.com/time_series?symbol=SPX,IXIC,DJI&interval=1min&apikey=" + _twelveDataApiKey;
            var tdResponse = await _client.GetAsync(tdEndpoint);
            string tdJsonResponse = await tdResponse.Content.ReadAsStringAsync();
            var tdIndices = JsonConvert.DeserializeObject<TwelveData.TwelveDataIndices>(tdJsonResponse);
            IndicesOutput output = new IndicesOutput(); ;
            List<StockIndex> indexList = new List<StockIndex>()
            {
                new StockIndex()
                {
                    name = "DOW",
                    points = Convert.ToDouble(tdIndices?.Dow?.Values[0]?.Close)
                },
                new StockIndex()
                {
                    name = "S&P 500",
                    points = Convert.ToDouble(tdIndices?.Spx?.Values[0]?.Close)
                },
                new StockIndex()
                {
                    name = "NASDAQ Composite",
                    points = Convert.ToDouble(tdIndices?.Ixic?.Values[0]?.Close)
                }
            };
            output.indices = indexList;
            output.source = StatusMessages.TwelveDataTimeSeriesMessage;
            output.timestamp = DateTime.Now;

            return output;
        }

        public async Task<TwelveDataPrice> GetTwelveDataRealTimePrice(string symbol)
        {
            var price = await _twelveDataClient.GetRealTimePriceAsync(symbol);
            return price;
        }
    }
}
