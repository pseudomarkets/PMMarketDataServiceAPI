using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MessagePack;
using PMCommonApiModels.ResponseModels;
using PMCommonEntities.Models.CachedData;

namespace PMMarketDataServiceAPI.Aerospike.Implementation
{
    public static class MessagePackUtil
    {
        public static byte[] SerializeDetailedQuote(DetailedQuoteOutput detailedQuote)
        {
            CachedDetailedQuote cachedDetailedQuote = new CachedDetailedQuote()
            {
                Symbol = detailedQuote.symbol,
                Name = detailedQuote.name,
                Timestamp = detailedQuote.timestamp,
                Open = detailedQuote.open,
                Close = detailedQuote.close,
                High = detailedQuote.high,
                Low = detailedQuote.low,
                Volume = detailedQuote.volume,
                PreviousClose = detailedQuote.previousClose,
                Change = detailedQuote.change,
                ChangePercentage = detailedQuote.changePercentage
            };

            var serializedData = MessagePackSerializer.Serialize(cachedDetailedQuote);

            return serializedData;
        }

        public static DetailedQuoteOutput DeserializeDetailedQuote(byte[] buffer)
        {
            var deserializedData = MessagePackSerializer.Deserialize<CachedDetailedQuote>(buffer);

            DetailedQuoteOutput detailedQuote = new DetailedQuoteOutput()
            {
                symbol = deserializedData.Symbol,
                name = deserializedData.Name,
                timestamp = deserializedData.Timestamp,
                open = deserializedData.Open,
                close = deserializedData.Close,
                high = deserializedData.High,
                low = deserializedData.Low,
                volume = deserializedData.Volume,
                previousClose = deserializedData.PreviousClose,
                changePercentage = deserializedData.ChangePercentage,
                change = deserializedData.Change
            };

            return detailedQuote;
        }
    }
}
