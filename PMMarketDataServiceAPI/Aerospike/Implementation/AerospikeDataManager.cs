using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aerospike.Client;
using MessagePack;
using PMCommonApiModels.ResponseModels;
using PMCommonEntities.Models.PseudoXchange;
using PMMarketDataServiceAPI.Aerospike.Interfaces;
using Log = Serilog.Log;

namespace PMMarketDataServiceAPI.Aerospike.Implementation
{
    public class AerospikeDataManager : IAerospikeDataManager
    {
        private readonly AerospikeClient _aerospikeClient;
        private readonly WritePolicy _writePolicy;

        public AerospikeDataManager(string hostname, int port, int cacheTtl)
        {
            _aerospikeClient = new AerospikeClient(hostname, port);
            _writePolicy = new WritePolicy()
            {
                expiration = cacheTtl
            };
        }

        public double GetCachedPrice(string symbol, string setName, string binName)
        {
            double cachedPrice = -1;

            try
            {
                Key recordKey = new Key(XchangeInMemNamespace.Namespace, setName, symbol);
                var record = _aerospikeClient.Get(new Policy(), recordKey);

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
                var record = _aerospikeClient.Get(new Policy(), recordKey);

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
                var record = _aerospikeClient.Get(new Policy(), recordKey);

                if (record != null)
                {
                    record.bins.TryGetValue(XchangeInMemNamespace.SetDetailedQuoteCache.CachedQuoteBin,
                        out var serializedDetailedQuote);

                    var buffer = (byte[]) serializedDetailedQuote;

                    detailedQuote = MessagePackUtil.DeserializeDetailedQuote(buffer);

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
                var serializedQuote = MessagePackUtil.SerializeDetailedQuote(detailedQuote);
                Bin detailedQuoteBin = new Bin(XchangeInMemNamespace.SetDetailedQuoteCache.CachedQuoteBin, serializedQuote);

                _aerospikeClient.Put(_writePolicy, recordKey, detailedQuoteBin);
            }
            catch (Exception e)
            {
                Log.Error(e, $"{nameof(SetCachedDetailedQuote)}");
            }
        }
    }
}
