using System.Collections.Generic;

namespace PMMarketDataService.DataProvider.CacheService.Interfaces
{
    public interface ICacheControlService
    {
        IEnumerable<string> GetCacheDisabledSymbolsList();

        void AppendToCacheDisabledSymbolsList(IEnumerable<string> symbols);

        void SetGlobalCacheDisableStatus(bool status);

        bool GetGlobalCacheDisableStatus();

        void ClearCacheDisabledSymbolsList();
    }
}
