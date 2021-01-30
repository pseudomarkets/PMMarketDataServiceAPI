using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using PMCommonEntities.Models.HistoricalData;
using PMMarketDataServiceAPI.MongoDb.Interfaces;
using Serilog;

namespace PMMarketDataServiceAPI.MongoDb.Implementation
{
    public class MongoDataManager : IMongoDataManager
    {
        private readonly IMongoClient _mongoClient;
        private readonly IMongoDatabase _mongoDatabase;
        private readonly IMongoCollection<BsonDocument> _mongoCollection;
        public MongoDataManager(string connectionString)
        {
            _mongoClient = new MongoClient(connectionString);
            _mongoDatabase = _mongoClient.GetDatabase("PseudoMarketsDB");
            _mongoCollection = _mongoDatabase.GetCollection<BsonDocument>("HistoricalStockData");
        }

        public HistoricalStockData GetHistoricalStockData(string symbol, string date)
        {
            try
            {
                var historicalDataDate = DateTime.ParseExact(date, "yyyyMMdd", CultureInfo.InvariantCulture);

                var symbolFilter = Builders<BsonDocument>.Filter.Eq("Symbol", symbol);
                var dateFilter = Builders<BsonDocument>.Filter.Eq("Date", historicalDataDate);

                var historicalDataDoc = _mongoCollection.Find(symbolFilter & dateFilter).FirstOrDefault();

                if (historicalDataDoc != null)
                {
                    var historicalData = BsonSerializer.Deserialize<HistoricalStockData>(historicalDataDoc);
                    return historicalData;
                }
                else
                {
                    return new HistoricalStockData();
                }
            }
            catch (Exception e)
            {
                Log.Error(e, $"{nameof(GetHistoricalStockData)}");
                return new HistoricalStockData();
            }
        }

        public void SaveHistoricalStockData(HistoricalStockData historicalStockData)
        {
            try
            {
                var replaceOptions = new ReplaceOptions()
                {
                    IsUpsert = true
                };

                var symbolAndDateFilter = Builders<BsonDocument>.Filter.Eq("Symbol", historicalStockData.Symbol) &
                                          Builders<BsonDocument>.Filter.Eq("Date", historicalStockData.Date);

                var historicalDataAsBson = historicalStockData.ToBsonDocument();

                _mongoCollection.ReplaceOne(symbolAndDateFilter, historicalDataAsBson, replaceOptions);
            }
            catch (Exception e)
            {
                Log.Error(e, $"{nameof(SaveHistoricalStockData)}");
            }
        }
    }
}
