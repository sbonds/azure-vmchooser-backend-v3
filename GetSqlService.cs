using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Collections.Generic;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System.Text;
using System.ComponentModel.DataAnnotations;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

namespace vmchooser
{
    [BsonIgnoreExtraElements] // Ignore all non-declared objects
    public class SqlService
    {
        /*
        {
	        "_id" : "sql-elastic-vcore-business-critical-backup-storage-lrs-asia-pacific-east",
	        "name" : "elastic-vcore-business-critical-backup-storage-lrs",
	        "region" : "asia-pacific-east",
	        "type" : "sql-vcore",
	        "tier" : "business-critical-backup",
	        "purchasemodel" : "elastic",
	        "contract" : "payg",
	        "ahub" : "no",
	        "compute" : "no",
	        "cores" : 0,
	        "memory" : 0,
	        "maxstorage" : 999999,
	        "maxiops" : 0,
	        "maxthroughput" : 0,
	        "storage" : 1,
            "price" : 0.15,
	        "priceUSD" : 0.15,
	        "priceEUR" : 0.126495,
	        "priceGBP" : 0.11179635,
	        "priceAUD" : 0.191055,
	        "priceJPY" : 16.8,
	        "priceCAD" : 0.182385,
	        "priceDKK" : 0.94401,
	        "priceCHF" : 0.147615,
	        "priceSEK" : 1.3105561499999998,
	        "priceIDR" : 2049,
	        "priceINR" : 9.9144375
        }
        */

        [Display(Description = "The name of the SQL Service")]
        [BsonElement("name")]
        public string Name { get; set; }

        [Display(Description = "The region linked to the pricing & availability of this SQL Service")]
        [BsonElement("region")]
        public string Region { get; set; }

        [Display(Description = "Take the Hybrid Use Benefit into account?")]
        [BsonElement("ahub")]
        public string AHUB { get; set; }

        [Display(Description = "The contract used for the pricing this SQL Service")]
        [BsonElement("contract")]
        public string Contract { get; set; }

        [Display(Description = "The purchase model for this SQL Service")]
        [BsonElement("purchasemodel")]
        public string PurchaseModel { get; set; }

        [Display(Description = "The tier of the SQL Service")]
        [BsonElement("tier")]
        public string Tier { get; set; }

        [Display(Description = "# of cores")]
        [BsonElement("cores")]
        public Int16 Cores { get; set; }

        [Display(Description = "# of memory (in GB)")]
        [BsonElement("memory")]
        public Decimal Memory { get; set; }

        [Display(Description = "Max netto capacity (in GB)")]
        [BsonElement("maxstorage")]
        public Decimal MaxStorage { get; set; }

        [Display(Description = "Max IOPS")]
        [BsonElement("maxiops")]
        public Decimal MaxIops { get; set; }

        [Display(Description = "Max throughput (in MB/s)")]
        [BsonElement("maxthroughput")]
        public Decimal MaxThroughput { get; set; }

        [Display(Description = "The price per hour for this VMsize expressed in the indicated currency")]
        [BsonElement("price")]
        [BsonRepresentation(BsonType.Decimal128, AllowTruncation = true)]
        public Decimal Price { get; set; }

        [Display(Description = "The currency used for the indicated pricing")]
        public String Currency = "USD";

        [Display(Description = "Price in US Dollar")]
        [BsonElement("price_USD")]
        [BsonRepresentation(BsonType.Decimal128, AllowTruncation = true)]
        public Decimal price_USD { get; set; }

        [Display(Description = "Price in Euro")]
        [BsonElement("price_EUR")]
        [BsonRepresentation(BsonType.Decimal128, AllowTruncation = true)]
        public Decimal price_EUR { get; set; }

        [Display(Description = "Price in GB Pound")]
        [BsonElement("price_GBP")]
        [BsonRepresentation(BsonType.Decimal128, AllowTruncation = true)]
        public Decimal price_GBP { get; set; }

        [Display(Description = "Price in Australian Dollar")]
        [BsonElement("price_AUD")]
        [BsonRepresentation(BsonType.Decimal128, AllowTruncation = true)]
        public Decimal price_AUD { get; set; }

        [Display(Description = "Price in Japanese Yen")]
        [BsonElement("price_JPY")]
        [BsonRepresentation(BsonType.Decimal128, AllowTruncation = true)]
        public Decimal price_JPY { get; set; }

        [Display(Description = "Price in Canadian Dollar")]
        [BsonElement("price_CAD")]
        [BsonRepresentation(BsonType.Decimal128, AllowTruncation = true)]
        public Decimal price_CAD { get; set; }

        [Display(Description = "Price in Danish Krones")]
        [BsonElement("price_DKK")]
        [BsonRepresentation(BsonType.Decimal128, AllowTruncation = true)]
        public Decimal price_DKK { get; set; }

        [Display(Description = "Price in Swiss Franks")]
        [BsonElement("price_CHF")]
        [BsonRepresentation(BsonType.Decimal128, AllowTruncation = true)]
        public Decimal price_CHF { get; set; }

        [Display(Description = "Price in Swedish Krones")]
        [BsonElement("price_SEK")]
        [BsonRepresentation(BsonType.Decimal128, AllowTruncation = true)]
        public Decimal price_SEK { get; set; }

        [Display(Description = "Price in Indonese Rupees")]
        [BsonElement("price_IDR")]
        [BsonRepresentation(BsonType.Decimal128, AllowTruncation = true)]
        public Decimal price_IDR { get; set; }

        [Display(Description = "Price in Indian Rupees")]
        [BsonElement("price_INR")]
        [BsonRepresentation(BsonType.Decimal128, AllowTruncation = true)]
        public Decimal price_INR { get; set; }

        [Display(Description = "Price in Russian Ruble")]
        [BsonElement("price_RUB")]
        [BsonRepresentation(BsonType.Decimal128, AllowTruncation = true)]
        public Decimal price_RUB { get; set; }

        // Set the Price & Currency on a requested currency name
        public void setCurrency(string currency)
        {
            Currency = currency;
            Price = Convert.ToDecimal(this.GetType().GetProperty("price_" + currency).GetValue(this, null));
        }
    }

    public static class GetSqlService
    {
        [FunctionName("GetSqlService")]
        [Display(Name = "GetSqlService", Description = "Find the best SQL Service for your given specifications")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequest req, ILogger log)
        {
            string databaseName = Environment.GetEnvironmentVariable("cosmosdbDatabaseName");
            string collectionName = Environment.GetEnvironmentVariable("cosmosdbCollectionName");
            string mongodbConnectionString = Environment.GetEnvironmentVariable("cosmosdbMongodbConnectionString");

            // This endpoint is valid for all MongoDB
            var client = new MongoClient(mongodbConnectionString);
            var database = client.GetDatabase(databaseName);
            var collection = database.GetCollection<BsonDocument>(collectionName);

            // Cores (Min) #
            decimal cores = Convert.ToDecimal(GetParameter("cores", "0", req));
            cores = SetMinimum(cores, 0);
            log.LogInformation("Cores : " + cores.ToString());
            // Memory (Min) #
            decimal memory = Convert.ToDecimal(GetParameter("memory", "0", req));
            memory = SetMinimum(memory, 0);
            log.LogInformation("Memory : " + memory.ToString());
            // IOPS (Min) #
            decimal iops = Convert.ToDecimal(GetParameter("iops", "-127", req));
            iops = SetMinimum(iops, -127);
            log.LogInformation("IOPS : " + iops.ToString());
            // Throughput (Min) #
            decimal throughput = Convert.ToDecimal(GetParameter("throughput", "-127", req));
            throughput = SetMinimum(throughput, -127);
            log.LogInformation("Throughput : " + throughput.ToString());
            // Data (Disk Capacity) (Min) #
            decimal data = Convert.ToDecimal(GetParameter("data", "0", req));
            data = SetMinimum(data, 0);
            log.LogInformation("Data : " + data.ToString());
            // Region #
            string region = GetParameter("region", "europe-west", req).ToLower();
            log.LogInformation("Region : " + region.ToString());
            // Currency #
            string currency = GetParameter("currency", "EUR", req).ToUpper();
            log.LogInformation("Currency : " + currency.ToString());
            // Contract #
            string contract = GetParameter("contract", "payg", req).ToLower();
            log.LogInformation("Contract : " + contract.ToString());
            // AHUB #
            string ahub = GetParameter("ahub", "no", req).ToLower();
            log.LogInformation("AHUB : " + ahub.ToString());
            // Results (Max) #
            decimal results = Convert.ToDecimal(GetParameter("maxresults", "1", req));
            results = SetMinimum(results, 1);
            results = SetMaximum(results, 100);
            log.LogInformation("Results : " + results.ToString());

            // Initialize results object
            List<SqlService> documents = new List<SqlService>();

            // Loop over the various purchase models
            List<String> names = new List<String>();
            names.Add("single");
            names.Add("elastic");
            names.Add("managed");

            names.ForEach(delegate (String purchasemodel) {
                var filterBuilder = Builders<BsonDocument>.Filter;
                var filter = filterBuilder.Eq("type", "sql-vcore")
                  & filterBuilder.Gte("cores", Convert.ToInt16(cores))
                  & filterBuilder.Gte("memory", Convert.ToInt16(memory))
                  & filterBuilder.Gte("maxiops", Convert.ToInt16(iops))
                  & filterBuilder.Gte("maxstorage", Convert.ToInt16(data))
                  & filterBuilder.Gte("maxthroughput", Convert.ToInt16(throughput))
                  & filterBuilder.Eq("purchasemodel", purchasemodel)
                  & filterBuilder.Eq("compute", "yes")
                  & filterBuilder.Eq("region", region)
                  & filterBuilder.Eq("ahub", ahub)
                  & filterBuilder.Eq("contract", contract)
                ;
                var sort = Builders<BsonDocument>.Sort.Ascending("price");
                var cursor = collection.Find<BsonDocument>(filter).Sort(sort).Limit(Convert.ToInt16(results)).ToCursor();
                foreach (var document in cursor.ToEnumerable())
                {
                    // Get RequestCharge
                    var LastRequestStatistics = database.RunCommand<BsonDocument>(new BsonDocument { { "getLastRequestStatistics", 1 } });
                    double RequestCharge = (double)LastRequestStatistics["RequestCharge"];
                    log.LogMetric("RequestCharge", RequestCharge);
                    // Get Document
                    // log.LogInformation(document.ToString());
                    SqlService mySqlService = BsonSerializer.Deserialize<SqlService>(document);
                    mySqlService.setCurrency(currency);
                    documents.Add(mySqlService);
                }

            });

            // Return results
            // Convert to JSON & return it
            var json = JsonConvert.SerializeObject(documents, Formatting.Indented);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }

        static public string GetParameter(string name, string defaultvalue, HttpRequest req)
        {
            string value = req.Query[name]; ;
            if (String.IsNullOrEmpty(value))
            {
                value = defaultvalue;
            }

            return value;
        }

        static public string[] YesNoAll(string value)
        {
            string[] response = new string[2];

            switch (value.ToLower())
            {
                case "yes":
                    response[0] = "Yes";
                    response[1] = "Yes";
                    break;
                case "no":
                    response[0] = "No";
                    response[1] = "No";
                    break;
                default:
                    response[0] = "Yes";
                    response[1] = "No";
                    break;
            }

            return response;
        }

        static public decimal SetMinimum(decimal value, decimal minimumvalue)
        {
            if (value < minimumvalue) { value = minimumvalue; }
            return value;
        }
        static public decimal SetMaximum(decimal value, decimal maximumvalue)
        {
            if (value > maximumvalue) { value = maximumvalue; }
            return value;
        }
    }
}
