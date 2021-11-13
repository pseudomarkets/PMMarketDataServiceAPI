using System;
using System.Collections.Generic;
using System.Linq;
using Aerospike.Client;
using PMCommonApiModels.ResponseModels;
using PMCommonEntities.Models.PseudoXchange;
using PMMarketDataService.DataProvider.CacheService.Interfaces;
using Log = Serilog.Log;

namespace PMMarketDataService.DataProvider.CacheService.Implementations
{
    public class AerospikeDataManager : IAerospikeDataManager, ICacheControlService
    {
        private readonly AerospikeClient _aerospikeClient;
        private readonly WritePolicy _writePolicy;
        private readonly WritePolicy _cacheControlPolicy;
        private readonly Policy _readPolicy;

        public AerospikeDataManager(string hostname, int port, int cacheTtl)
        {
            _aerospikeClient = new AerospikeClient(hostname, port);

            _readPolicy = new Policy();

            _writePolicy = new WritePolicy()
            {
                expiration = cacheTtl
            };

            _cacheControlPolicy = new WritePolicy()
            {
                expiration = -1
            };
        }

        public double GetCachedPrice(string symbol, string setName, string binName)
        {
            double cachedPrice = -1;

            try
            {
                Key recordKey = new Key(XchangeInMemNamespace.Namespace, setName, symbol);
                var record = _aerospikeClient.Get(_readPolicy, recordKey);

                if (record != null)
                {
                    record.bins.TryGetValue(binName, out var price);
                    if (price != null)
                    {
                        cachedPrice = Convert.ToDouble(price);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e, $"{nameof(GetCachedPrice)}");
            }

            return cachedPrice;
        }

        public void SetCachedPrice(string symbol, double price, string setName, string binName)
        {
            try
            {
                Key recordKey = new Key(XchangeInMemNamespace.Namespace, setName, symbol);
                Bin priceBin = new Bin(binName, price);
                _aerospikeClient.Put(_writePolicy, recordKey, priceBin);
            }
            catch (Exception e)
            {
                Log.Error(e, $"{nameof(SetCachedPrice)}");
            }
        }

        public Dictionary<string, double> GetCachedIndices()
        {
            Dictionary<string, double> cachedIndices = new Dictionary<string, double>();

            try
            {
                Key recordKey = new Key(XchangeInMemNamespace.Namespace, XchangeInMemNamespace.SetIndicesCache.Set, "idxCache");
                var record = _aerospikeClient.Get(_readPolicy, recordKey);

                if (record != null)
                {
                    record.bins.TryGetValue(XchangeInMemNamespace.SetIndicesCache.DowPointsBin, out var dow);
                    record.bins.TryGetValue(XchangeInMemNamespace.SetIndicesCache.Sp500PointsBin, out var sp500);
                    record.bins.TryGetValue(XchangeInMemNamespace.SetIndicesCache.NasdaqPointsBin, out var nasdaq);

                    double dowPoints = Convert.ToDouble(dow);
                    double sp500Points = Convert.ToDouble(sp500);
                    double nasdaqPoints = Convert.ToDouble(nasdaq);

                    cachedIndices.Add("DOW", dowPoints);
                    cachedIndices.Add("S&P 500", sp500Points);
                    cachedIndices.Add("NASDAQ Composite", nasdaqPoints);
                }

            }
            catch (Exception e)
            {
                Log.Error(e, $"{nameof(GetCachedIndices)}");
            }

            return cachedIndices;
        }

        public void SetCachedIndices(double dowPoints, double sp500Points, double nasdaqPoints)
        {
            try
            {
                Key recordKey = new Key(XchangeInMemNamespace.Namespace, XchangeInMemNamespace.SetIndicesCache.Set, "idxCache");
                Bin dowBin = new Bin(XchangeInMemNamespace.SetIndicesCache.DowPointsBin, dowPoints);
                Bin sp500Bin = new Bin(XchangeInMemNamespace.SetIndicesCache.Sp500PointsBin, sp500Points);
                Bin nasdaqBin = new Bin(XchangeInMemNamespace.SetIndicesCache.NasdaqPointsBin, nasdaqPoints);

                _aerospikeClient.Put(_writePolicy, recordKey, dowBin, nasdaqBin, sp500Bin);
            }
            catch (Exception e)
            {
                Log.Error(e, $"{nameof(SetCachedIndices)}");
            }
        }

        public DetailedQuoteOutput GetCachedDetailedQuote(string symbol)
        {
            DetailedQuoteOutput detailedQuote = new DetailedQuoteOutput();

            try
            {
                Key recordKey = new Key(XchangeInMemNamespace.Namespace, XchangeInMemNamespace.SetDetailedQuoteCache.Set, symbol);
                var record = _aerospikeClient.Get(_readPolicy, recordKey);

                if (record != null)
                {
                    record.bins.TryGetValue(XchangeInMemNamespace.SetDetailedQuoteCache.CachedQuoteBin,
                        out var serializedDetailedQuote);

                    detailedQuote = MessagePackUtil.DeserializeDetailedQuote(serializedDetailedQuote as byte[]);

                    return detailedQuote;
                }
            }
            catch (Exception e)
            {
                Log.Error(e, $"{nameof(GetCachedDetailedQuote)}");
            }

            return detailedQuote;
        }

        public void SetCachedDetailedQuote(DetailedQuoteOutput detailedQuote)
        {
            try
            {
                Key recordKey = new Key(XchangeInMemNamespace.Namespace, XchangeInMemNamespace.SetDetailedQuoteCache.Set, detailedQuote.symbol);
                var serializedQuote = detailedQuote.SerializeDetailedQuote();
                Bin detailedQuoteBin = new Bin(XchangeInMemNamespace.SetDetailedQuoteCache.CachedQuoteBin, serializedQuote);

                _aerospikeClient.Put(_writePolicy, recordKey, detailedQuoteBin);
            }
            catch (Exception e)
            {
                Log.Error(e, $"{nameof(SetCachedDetailedQuote)}");
            }
        }

        public IEnumerable<string> GetCacheDisabledSymbolsList()
        {
            List<string> symbols = new List<string>();

            var key = new Key(XchangeInMemNamespace.Namespace, XchangeInMemNamespace.SetCacheControl.Set,
                XchangeInMemNamespace.SetCacheControl.RecordKey);

            var record = _aerospikeClient.Operate(_cacheControlPolicy, key,
                ListOperation.GetRange(XchangeInMemNamespace.SetCacheControl.PassThruSymbolListBin, 0));

            if (record?.bins != null)
            {
                var list = record.GetList(XchangeInMemNamespace.SetCacheControl.PassThruSymbolListBin);

                foreach (var listObject in list)
                {
                    symbols.Add(Convert.ToString(listObject));
                }
            }

            return symbols;
        }

        public void AppendToCacheDisabledSymbolsList(IEnumerable<string> symbols)
        {
            var key = new Key(XchangeInMemNamespace.Namespace, XchangeInMemNamespace.SetCacheControl.Set,
                XchangeInMemNamespace.SetCacheControl.RecordKey);

            _aerospikeClient.Operate(_cacheControlPolicy, key,
                ListOperation.AppendItems(XchangeInMemNamespace.SetCacheControl.PassThruSymbolListBin, symbols.ToList()));
        }

        public void ClearCacheDisabledSymbolsList()
        {
            var key = new Key(XchangeInMemNamespace.Namespace, XchangeInMemNamespace.SetCacheControl.Set,
                XchangeInMemNamespace.SetCacheControl.RecordKey);

            _aerospikeClient.Operate(_cacheControlPolicy, key,
                ListOperation.Clear(XchangeInMemNamespace.SetCacheControl.PassThruSymbolListBin), ListOperation.Create(XchangeInMemNamespace.SetCacheControl.PassThruSymbolListBin, ListOrder.UNORDERED, false));
        }

        public void SetGlobalCacheDisableStatus(bool status)
        {
            var key = new Key(XchangeInMemNamespace.Namespace, XchangeInMemNamespace.SetCacheControl.Set,
                XchangeInMemNamespace.SetCacheControl.RecordKey);

            var globalCacheDisableStatusBin =
                new Bin(XchangeInMemNamespace.SetCacheControl.GlobalCacheDisableBin, status);

            _aerospikeClient.Put(_cacheControlPolicy, key, globalCacheDisableStatusBin);
        }

        public bool GetGlobalCacheDisableStatus()
        {
            bool status = false;

            var key = new Key(XchangeInMemNamespace.Namespace, XchangeInMemNamespace.SetCacheControl.Set,
                XchangeInMemNamespace.SetCacheControl.RecordKey);

            var record = _aerospikeClient.Get(_readPolicy, key);

            if (record?.bins != null)
            {
                status = record.GetBool(XchangeInMemNamespace.SetCacheControl.GlobalCacheDisableBin);
            }

            return status;
        }

        public bool IsConnected()
        {
            return _aerospikeClient.Connected;
        }
    }
}
