using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PMCommonEntities.Models.HistoricalData;

namespace PMMarketDataServiceAPI.MongoDb.Interfaces
{
    public interface IMongoDataManager
    {
        public HistoricalStockData GetHistoricalStockData(string symbol, string date);
        public void SaveHistoricalStockData(HistoricalStockData historicalStockData);
    }
}
