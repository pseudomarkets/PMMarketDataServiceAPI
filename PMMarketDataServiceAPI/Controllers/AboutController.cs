using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PMMarketDataServiceAPI.Models;

namespace PMMarketDataServiceAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AboutController : ControllerBase
    {
        private readonly DataServiceConfig _dataServiceConfig;
        public AboutController(DataServiceConfig dataServiceConfig)
        {
            _dataServiceConfig = dataServiceConfig;
        }

        // GET: api/About/
        [AllowAnonymous]
        [HttpGet]
        public string AboutService()
        {
            return
                $"Market Data Service\nVersion: {_dataServiceConfig.ServiceVersion}\n(c) 2019 - {DateTime.Now.Year} Pseudo Markets";
        }
    }
}
