using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Collections.Generic;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System.Text;
using System.Web.Http.Description;
using System.ComponentModel.DataAnnotations;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

namespace vmchooser
{
    [BsonIgnoreExtraElements] // Ignore all non-declared objects
    public class SqlServiceStorage
    {
        /*
        {
	        "_id" : "sql-elastic-vcore-general-purpose-backup-storage-lrs-us-central",
	        "name" : "elastic-vcore-general-purpose-backup-storage-lrs",
	        "region" : "us-central",
	        "type" : "sql-vcore",
	        "tier" : "general-purpose-backup",
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
	        "price" : 0.12,
	        "price_USD" : 0.12,
	        "price_EUR" : 0.10119600000000001,
	        "price_GBP" : 0.08943708,
	        "price_AUD" : 0.152844,
	        "price_JPY" : 13.44,
	        "price_CAD" : 0.14590799999999998,
	        "price_DKK" : 0.755208,
	        "price_CHF" : 0.11809199999999999,
	        "price_SEK" : 1.04844492,
	        "price_IDR" : 1639.2,
	        "price_INR" : 7.93155
        }
        */

        [Display(Description = "The name of the SQL Service")]
        [BsonElement("name")]
        public string Name { get; set; }

        [Display(Description = "The region linked to the pricing & availability of this SQL Service")]
        [BsonElement("region")]
        public string Region { get; set; }

        [Display(Description = "The purchase model for this SQL Service")]
        [BsonElement("purchasemodel")]
        public string PurchaseModel { get; set; }

        [Display(Description = "The tier of the SQL Service")]
        [BsonElement("tier")]
        public string Tier { get; set; }

        [Display(Description = "The price per GB per month expressed in the indicated currency")]
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

        public static class GetSqlServiceStorage
        {
            [FunctionName("GetSqlServiceStorage")]
            [Display(Name = "GetSqlServiceStorage", Description = "Find the storage cost of your SQL Service given your specifications")]
            public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
            {
                string databaseName = Environment.GetEnvironmentVariable("cosmosdbDatabaseName");
                string collectionName = Environment.GetEnvironmentVariable("cosmosdbCollectionName");
                string mongodbConnectionString = Environment.GetEnvironmentVariable("cosmosdbMongodbConnectionString");

                // This endpoint is valid for all MongoDB
                var client = new MongoClient(mongodbConnectionString);
                var database = client.GetDatabase(databaseName);
                var collection = database.GetCollection<BsonDocument>(collectionName);

                // Get Parameters
                dynamic contentdata = await req.Content.ReadAsAsync<object>();
                // Region #
                string region = GetParameter("region", "europe-west", req).ToLower();
                log.Info("Region : " + region.ToString());
                // Currency #
                string currency = GetParameter("currency", "EUR", req).ToUpper();
                log.Info("Currency : " + currency.ToString());
                // Purchase Model
                string purchasemodel = GetParameter("purchasemodel", "managed", req).ToLower();
                log.Info("Purchase Model : " + purchasemodel.ToString());
                // tier
                string tier = GetParameter("tier", "general-purpose", req).ToLower();
                log.Info("Tier : " + tier.ToString());

                // Load Application Insights
                string ApplicationInsightsKey = TelemetryConfiguration.Active.InstrumentationKey = System.Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY", EnvironmentVariableTarget.Process);
                TelemetryClient telemetry = new TelemetryClient() { InstrumentationKey = ApplicationInsightsKey };

                // Initialize results object
                List<SqlServiceStorage> documents = new List<SqlServiceStorage>();

                // Get Storage Pricing
                var filterBuilder = Builders<BsonDocument>.Filter;
                var filter = filterBuilder.Eq("type", "sql-vcore")
                    & filterBuilder.Eq("purchasemodel", purchasemodel)
                    & filterBuilder.Eq("compute", "no")
                    & filterBuilder.Eq("region", region)
                    & filterBuilder.Eq("tier", tier)
                ;
                var sort = Builders<BsonDocument>.Sort.Ascending("price");
                var cursor = collection.Find<BsonDocument>(filter).Sort(sort).Limit(1).ToCursor();
                foreach (var document in cursor.ToEnumerable())
                {
                    // Get RequestCharge
                    var LastRequestStatistics = database.RunCommand<BsonDocument>(new BsonDocument { { "getLastRequestStatistics", 1 } });
                    double RequestCharge = (double)LastRequestStatistics["RequestCharge"];
                    telemetry.TrackMetric("RequestCharge", RequestCharge);
                    // Get Document
                    SqlServiceStorage mySqlServiceStorage = BsonSerializer.Deserialize<SqlServiceStorage>(document);
                    mySqlServiceStorage.setCurrency(currency);
                    documents.Add(mySqlServiceStorage);
                }

                // Return results
                // Convert to JSON & return it
                var json = JsonConvert.SerializeObject(documents, Formatting.Indented);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
            }
            static public string GetParameter(string name, string defaultvalue, HttpRequestMessage req)
            {
                string value = req.GetQueryNameValuePairs()
                    .FirstOrDefault(q => string.Compare(q.Key, name, true) == 0)
                    .Value;
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
}
