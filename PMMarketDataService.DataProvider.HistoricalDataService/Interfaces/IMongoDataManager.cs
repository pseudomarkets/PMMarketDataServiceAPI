using PMCommonEntities.Models.HistoricalData;

namespace PMMarketDataService.DataProvider.HistoricalDataService.Interfaces
{
    public interface IMongoDataManager
    {
        public HistoricalStockData GetHistoricalStockData(string symbol, string date);
        public void SaveHistoricalStockData(HistoricalStockData historicalStockData);
    }
}
