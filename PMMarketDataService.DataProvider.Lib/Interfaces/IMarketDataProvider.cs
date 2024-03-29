﻿using System.Threading.Tasks;
using PMCommonApiModels.ResponseModels;
using TwelveDataSharp.Library.ResponseModels;

namespace PMMarketDataService.DataProvider.Lib.Interfaces
{
    public interface IMarketDataProvider
    {
        Task<AlphaVantage.AlphaVantageGlobalQuote> GetAlphaVantageGlobalQuote(string symbol);
        Task<IexCloudTops> GetIexTopsData(string symbol);
        Task<DetailedQuoteOutput> GetTwelveDataDetailedQuote(string symbol, string interval);
        Task<IndicesOutput> GetTwelveDataIndices();
        Task<TwelveDataPrice> GetTwelveDataRealTimePrice(string symbol);
    }
}
