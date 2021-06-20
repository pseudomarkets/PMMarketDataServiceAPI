using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PMMarketDataService.DataProvider.CacheService.Implementations;
using PMMarketDataServiceAPI.Models;


namespace PMMarketDataServiceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CacheControlController : ControllerBase
    {
        private readonly AerospikeDataManager _aerospikeConnectionManager;

        public CacheControlController(AerospikeDataManager aerospikeDataManager)
        {
            _aerospikeConnectionManager = aerospikeDataManager;
        }

        // GET: api/CacheControl/GetGlobalCacheStatus
        [HttpGet]
        [Route("GetGlobalCacheStatus")]
        public GlobalCacheControlResponse GetGlobalCacheControlStatus()
        {
           var status = _aerospikeConnectionManager.GetGlobalCacheDisableStatus();

           return new GlobalCacheControlResponse()
           {
               IsGlobalCacheDisabled = status
           };
        }

        // POST: /api/CacheControl/SetGlobalCacheStatus
        [HttpPost]
        [Route("SetGlobalCacheStatus")]
        public void SetGlobalCacheControlStatus([FromHeader] bool status)
        {
            _aerospikeConnectionManager.SetGlobalCacheDisableStatus(status);
        }

        // POST: api/CacheControl/AppendToSymbolsList
        [HttpPost]
        [Route("AppendToSymbolsList")]
        public void AppendToSymbolsList([FromHeader] string symbols)
        {
            var symbolsList = symbols.Split(",").ToList().Select(x => x.ToUpper()).ToList();

            _aerospikeConnectionManager.AppendToCacheDisabledSymbolsList(symbolsList);
        }

        // POST: /api/CacheControl/ClearSymbolsList
        [HttpPost]
        [Route("ClearSymbolsList")]
        public void ClearSymbolsList()
        {
            _aerospikeConnectionManager.ClearCacheDisabledSymbolsList();
        }

        // GET: /api/CacheControl/GetSymbolsList
        [HttpGet]
        [Route("GetSymbolsList")]
        public DisabledSymbolsListResponse GetSymbolsList()
        {
            var symbols = _aerospikeConnectionManager.GetCacheDisabledSymbolsList().ToList();

            return new DisabledSymbolsListResponse()
            {
                Symbols = symbols
            };
        }
    }
}
