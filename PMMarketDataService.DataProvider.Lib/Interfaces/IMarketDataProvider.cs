using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using PMCommonApiModels.ResponseModels;
using TwelveDataSharp.DataModels;

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
