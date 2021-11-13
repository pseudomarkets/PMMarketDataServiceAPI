namespace PMMarketDataService.DataProvider.Client.Implementation
{
    public static class EndpointDefinitions
    {
        public const string LatestPrice = "/api/MarketData/LatestPrice";
        public const string AggregatePrice = "/api/MarketData/AggregatePrice";
        public const string DetailedQuote = "/api/MarketData/DetailedQuote";
        public const string Indices = "/api/MarketData/Indices";
        public const string HistoricalData = "/api/HistoricalData/GetHistoricalData";
    }
}
