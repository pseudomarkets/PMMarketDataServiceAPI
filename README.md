# Market Data Service API

Real-time market data service and client for Pseudo Markets

# Requirements
* Pseudo Markets instance 
* MS SQL Server 2017+
* Mongo DB 4.x
* Aerospike CE 5.x

# Features
* Support for real-time market data from IEX, Twelve Data, and Alpha Vantage
* Includes easy-to-consume .NET Standard client for direct integration with the Unified API
* High-speed caching system powered by Aerospike to reduce outbound data provider API requests
* Historical data storage and retrieval for building performance projections and calculations

# Usage
The API can be called directly, or through the PMMarketDataService.DataProvider.Client (recommended)
 
NTLM or Basic Auth through IIS is recommended to secure the API from external use. 

(c) 2019 - 2021 Pseudo Markets
