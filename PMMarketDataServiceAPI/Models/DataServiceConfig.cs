namespace PMMarketDataServiceAPI.Models
{
    public class DataServiceConfig
    {
        public string IexApiKey { get; set; }
        public string TwelveDataApiKey { get; set; }
        public string AlphaVantageApiKey { get; set; }
        public int PriceCacheTtl { get; set; }
        public string ServiceVersion { get; set; }
    }
}
