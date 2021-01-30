using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using PMCommonApiModels.ResponseModels;
using PMCommonEntities.Models.HistoricalData;

namespace PMMarketDataService.DataProvider.Client.Interfaces
{
    public interface IMarketDataServiceClient
    {
        Task<LatestPriceOutput> GetLatestPrice(string symbol);
        Task<LatestPriceOutput> GetAggregatePrice(string symbol);
        Task<DetailedQuoteOutput> GetDetailedQuote(string symbol, string interval = "1min");
        Task<IndicesOutput> GetIndices();
        Task<HistoricalStockData> GetHistoricalData(string symbol, string date);
    }
}
