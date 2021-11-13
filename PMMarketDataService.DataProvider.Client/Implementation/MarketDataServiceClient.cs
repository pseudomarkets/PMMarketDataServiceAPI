using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PMCommonApiModels.ResponseModels;
using PMCommonEntities.Models.HistoricalData;
using PMMarketDataService.DataProvider.Client.Interfaces;

namespace PMMarketDataService.DataProvider.Client.Implementation
{
    public class MarketDataServiceClient : IMarketDataServiceClient
    {
        private readonly HttpClient _httpClient;
        private string _authHeader = string.Empty;

        public MarketDataServiceClient(HttpClient httpClient, string internalAuthUsername, string internalAuthPassword, string baseUrl)
        {
            _httpClient = httpClient;
            var byteArray = Encoding.ASCII.GetBytes($"{internalAuthUsername}:{internalAuthPassword}");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            _httpClient.BaseAddress = new Uri(baseUrl);
        }

        public async Task<LatestPriceOutput> GetLatestPrice(string symbol)
        {
            var response = await _httpClient.GetStringAsync($"{EndpointDefinitions.LatestPrice}/{symbol.ToUpper()}");
            var latestPrice = JsonConvert.DeserializeObject<LatestPriceOutput>(response);
            return latestPrice;
        }

        public async Task<LatestPriceOutput> GetAggregatePrice(string symbol)
        {
            var response = await _httpClient.GetStringAsync($"{EndpointDefinitions.AggregatePrice}/{symbol.ToUpper()}");
            var aggregatePrice = JsonConvert.DeserializeObject<LatestPriceOutput>(response);
            return aggregatePrice;
        }

        public async Task<DetailedQuoteOutput> GetDetailedQuote(string symbol, string interval = "1min")
        {
            var response = await _httpClient.GetStringAsync($"{EndpointDefinitions.DetailedQuote}/{symbol.ToUpper()}/{interval}");
            var detailedQuote = JsonConvert.DeserializeObject<DetailedQuoteOutput>(response);
            return detailedQuote;
        }

        public async Task<IndicesOutput> GetIndices()
        {
            var response = await _httpClient.GetStringAsync($"{EndpointDefinitions.Indices}");
            var indices = JsonConvert.DeserializeObject<IndicesOutput>(response);
            return indices;
        }

        public async Task<HistoricalStockData> GetHistoricalData(string symbol, string date)
        {
            var response = await _httpClient.GetStringAsync($"{EndpointDefinitions.HistoricalData}/{symbol.ToUpper()}/{date}");
            var historicalData = JsonConvert.DeserializeObject<HistoricalStockData>(response);
            return historicalData;
        }
    }
}
