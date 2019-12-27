using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System;
using MongoDB.Driver;
using MongoDB.Bson;
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
    public class VmBackupStorage
    {
        /*
        {
	        "_id" : "backup-storage-lrs-south-india",
	        "name" : "backup-storage-lrs-south-india",
	        "region" : "south-india",
	        "type" : "backup",
	        "dimension" : "backup-storage-lrs",
	        "incrementalsize" : "",
	        "discountsize" : "",
	        "discountrate" : "",
	        "price" : 0.0238,
	        "price_USD" : 0.0238,
	        "price_EUR" : 0.02007054,
	        "price_GBP" : 0.0177383542,
	        "price_AUD" : 0.030314060000000004,
	        "price_JPY" : 2.6656000000000004,
	        "price_CAD" : 0.028938420000000003,
	        "price_DKK" : 0.14978292,
	        "price_CHF" : 0.02342158,
	        "price_SEK" : 0.2079415758,
	        "price_IDR" : 325.108,
	        "price_INR" : 1.57309075
        }
        */

        [Display(Description = "The name of the backup storage item")]
        [BsonElement("name")]
        public string Name { get; set; }

        [Display(Description = "The region linked to the pricing & availability of this VM Size")]
        [BsonElement("region")]
        public string Region { get; set; }

        [Display(Description = "The region linked to the pricing & availability of this VM Size")]
        [BsonElement("dimension")]
        public string Dimension { get; set; }

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

        // Calculate Backup Size
        public decimal calculateBackupSize(decimal Size, decimal Daily, decimal Weekly, decimal Monthly, decimal Yearly, decimal Churn, decimal Compression)
        {
            return ( 
                (Size * ((100 - Compression) / 100))
                + (Daily * Size * (Churn / 100) * ((100 - Compression) / 100))
                + (Weekly * Size * (Churn / 100) * ((100 - Compression) / 100))
                + (Monthly * Size * (Churn / 100) * ((100 - Compression) / 100))
                + (Yearly * Size * (Churn / 100) * ((100 - Compression) / 100))
                );
        }
        public decimal calculateBackupStorageCost(decimal TotalSize) {
            return (TotalSize * Price);
        }

    }

    [BsonIgnoreExtraElements] // Ignore all non-declared objects
    public class VmBackupInstance
    {
        /*
        {
	        "_id" : "backup-instance-brazil-south",
	        "name" : "backup-instance-brazil-south",
	        "region" : "brazil-south",
	        "type" : "backup",
	        "dimension" : "backup-instance",
	        "incrementalsize" : 500,
	        "discountsize" : 50,
	        "discountrate" : 0.5,
	        "price" : 10,
	        "price_USD" : 10,
	        "price_EUR" : 8.433,
	        "price_GBP" : 7.4530899999999995,
	        "price_AUD" : 12.737,
	        "price_JPY" : 1120,
	        "price_CAD" : 12.158999999999999,
	        "price_DKK" : 62.934,
	        "price_CHF" : 9.841,
	        "price_SEK" : 87.37040999999999,
	        "price_IDR" : 136600,
	        "price_INR" : 660.9625
        }
        */

        [Display(Description = "The name of the backup storage item")]
        [BsonElement("name")]
        public string Name { get; set; }

        [Display(Description = "The region linked to the pricing & availability of this VM Size")]
        [BsonElement("region")]
        public string Region { get; set; }

        [Display(Description = "The storage item dimension")]
        [BsonElement("dimension")]
        public string Dimension { get; set; }

        [Display(Description = "The chunk size")]
        [BsonElement("incrementalsize")]
        [BsonRepresentation(BsonType.Decimal128, AllowTruncation = true)]
        public Decimal IncrementalSize { get; set; }

        [Display(Description = "The discount size")]
        [BsonElement("discountsize")]
        [BsonRepresentation(BsonType.Decimal128, AllowTruncation = true)]
        public Decimal DiscountSize { get; set; }

        [Display(Description = "The discount rate")]
        [BsonElement("discountrate")]
        [BsonRepresentation(BsonType.Decimal128, AllowTruncation = true)]
        public Decimal DiscountRate { get; set; }

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

        // Calculate Backup Instance Cost
        public decimal calculateBackupInstance(decimal Size)
        {
            if (DiscountSize >= Size)
            {
                return (DiscountRate * Price);
            }
            else {
                return (Math.Ceiling(Size / IncrementalSize) * Price);
            }
        }
    }

    public static class GetBackup
    {
        [FunctionName("GetBackup")]
        [Display(Name = "GetBackup", Description = "Get the cost for Azure backup given a set of specifications")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "options", Route = null)]HttpRequest req, ILogger log)
        {
            // CosmosDB Parameters, retrieved via environment variables
            string databaseName = Environment.GetEnvironmentVariable("cosmosdbDatabaseName");
            string collectionName = Environment.GetEnvironmentVariable("cosmosdbCollectionName");
            string mongodbConnectionString = Environment.GetEnvironmentVariable("cosmosdbMongodbConnectionString");

            // This endpoint is valid for all MongoDB
            var client = new MongoClient(mongodbConnectionString);
            var database = client.GetDatabase(databaseName);
            var collection = database.GetCollection<BsonDocument>(collectionName);

            // Backup Size (source)
            decimal size = Convert.ToDecimal(GetParameter("size", "0", req));
            size = SetMinimum(size, 0);
            log.LogInformation("Backup Size : " + size.ToString());
            // # of Daily backups
            decimal daily = Convert.ToDecimal(GetParameter("daily", "0", req));
            daily = SetMinimum(daily, 0);
            log.LogInformation("Daily : " + daily.ToString());
            // # of Weekly backups
            decimal weekly = Convert.ToDecimal(GetParameter("weekly", "0", req));
            weekly = SetMinimum(weekly, 0);
            log.LogInformation("Weekly : " + weekly.ToString());
            // # of Monthly backups
            decimal monthly = Convert.ToDecimal(GetParameter("monthly", "0", req));
            monthly = SetMinimum(monthly, 0);
            log.LogInformation("Monthly : " + monthly.ToString());
            // # of Yearly backups
            decimal yearly = Convert.ToDecimal(GetParameter("yearly", "0", req));
            yearly = SetMinimum(yearly, 0);
            log.LogInformation("Yearly : " + yearly.ToString());
            // Region #
            string region = GetParameter("region", "europe-west", req).ToLower();
            log.LogInformation("Region : " + region.ToString());
            // Currency #
            string currency = GetParameter("currency", "EUR", req).ToUpper();
            log.LogInformation("Currency : " + currency.ToString());
            // Resiliency
            string resiliency = GetParameter("resiliency", "lrs", req).ToLower();
            log.LogInformation("Resiliency : " + resiliency.ToString());
            // Churn Rate (%)
            decimal churn = Convert.ToDecimal(GetParameter("churn", "2", req));
            churn = SetMinimum(churn, 0);
            churn = SetMaximum(churn, 100);
            log.LogInformation("Churn : " + churn.ToString());
            // Compression Gain (%)
            decimal compression = Convert.ToDecimal(GetParameter("compression", "30", req));
            compression = SetMinimum(compression, 0);
            compression = SetMaximum(compression, 100);
            log.LogInformation("Compression : " + compression.ToString());

            // Create Return Object
            dynamic documents = new System.Dynamic.ExpandoObject();

            // Get Backup Instance Cost
            var filterBuilder = Builders<BsonDocument>.Filter;
            var filter = filterBuilder.Eq("type", "backup")
                        & filterBuilder.Eq("dimension", "backup-instance")
                        & filterBuilder.Eq("region", region)
                        ;
            var sort = Builders<BsonDocument>.Sort.Ascending("price");
            var cursor = collection.Find<BsonDocument>(filter).Sort(sort).Limit(1).ToCursor();
            foreach (var document in cursor.ToEnumerable())
            {
                // Get RequestCharge
                var LastRequestStatistics = database.RunCommand<BsonDocument>(new BsonDocument { { "getLastRequestStatistics", 1 } });
                double RequestCharge = (double)LastRequestStatistics["RequestCharge"];
                log.LogMetric("RequestCharge", RequestCharge);

                // Get Document
                log.LogInformation(document.ToString());
                VmBackupInstance myVmBackupInstance = BsonSerializer.Deserialize<VmBackupInstance>(document);
                myVmBackupInstance.setCurrency(currency);
                decimal VmBackupInstanceCost = myVmBackupInstance.calculateBackupInstance(size);
                documents.CostInstance = VmBackupInstanceCost.ToString();
            }

            // Get Backup Storage Cost
            filterBuilder = Builders<BsonDocument>.Filter;
            filter = filterBuilder.Eq("type", "backup")
                        & filterBuilder.Eq("dimension", "backup-storage-"+resiliency)
                        & filterBuilder.Eq("region", region)
                        ;
            sort = Builders<BsonDocument>.Sort.Ascending("price");
            cursor = collection.Find<BsonDocument>(filter).Sort(sort).Limit(1).ToCursor();
            foreach (var document in cursor.ToEnumerable())
            {
                // Get RequestCharge
                var LastRequestStatistics = database.RunCommand<BsonDocument>(new BsonDocument { { "getLastRequestStatistics", 1 } });
                double RequestCharge = (double)LastRequestStatistics["RequestCharge"];
                log.LogMetric("RequestCharge", RequestCharge);

                // Get Document
                log.LogInformation(document.ToString());
                VmBackupStorage myVmBackupStorage = BsonSerializer.Deserialize<VmBackupStorage>(document);
                myVmBackupStorage.setCurrency(currency);
                decimal VmBackupStorageSize = myVmBackupStorage.calculateBackupSize(size, daily, weekly, monthly, yearly, churn, compression);
                decimal VmBackupStorageCost = myVmBackupStorage.calculateBackupStorageCost(VmBackupStorageSize);
                documents.SizeTotal = VmBackupStorageSize.ToString();
                documents.CostStorage = VmBackupStorageCost.ToString();
            }

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
