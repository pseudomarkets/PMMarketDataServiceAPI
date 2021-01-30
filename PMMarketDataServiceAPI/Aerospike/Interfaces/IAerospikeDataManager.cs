using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aerospike.Client;

namespace PMMarketDataServiceAPI.Aerospike.Interfaces
{
    public interface IAerospikeDataManager
    {
        public double GetCachedPrice(string symbol, string setName, string binName);
        public void SetCachedPrice(string symbol, double price, string setName, string binName);
        public Dictionary<string, double> GetCachedIndices();
        public void SetCachedIndices(double dowPoints, double sp500Points, double nasdaqPoints);
    }
}
