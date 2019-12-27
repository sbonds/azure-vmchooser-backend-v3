using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Text;
using System.Web.Http.Description;
using System.ComponentModel.DataAnnotations;
using System;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using MongoDB.Bson.Serialization;
using System.Collections.Generic;

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

namespace vmchooser
{
    public class DiskConfig
    {
        [Display(Description = "The name of the disk")]
        public string DiskName { get; set; }

        [Display(Description = "The type of the disk")]
        public string DiskType { get; set; }

        [Display(Description = "The size of the disk")]
        public string DiskSize { get; set; }

        [Display(Description = "The number of the disks used in the ocnfig")]
        public decimal DiskCount { get; set; }

        [Display(Description = "The capacity of the disk (in GB)")]
        public decimal DiskCapacity { get; set; }

        [Display(Description = "The max. IOPS of the disk")]
        public decimal DiskIops { get; set; }

        [Display(Description = "The max. throughput of the disk (in MB/s)")]
        public decimal DiskThroughput { get; set; }

        [Display(Description = "The capacity of the entire configuration (in GB)")]
        public decimal DiskConfigCapacity { get; set; }

        [Display(Description = "The max. IOPS of the entire configuration")]
        public decimal DiskConfigIops { get; set; }

        [Display(Description = "The max. throughput of the entire configuration (in MB/s)")]
        public decimal DiskConfigThroughput { get; set; }

        [Display(Description = "The description of the configuration")]
        public string DiskConfigDescription { get; set; }

        [Display(Description = "The currency used to indicate the price")]
        public string DiskCurrency { get; set; }

        [Display(Description = "The price per month for the entire configuration")]
        public decimal DiskPrice { get; set; }

        // Set the Price & Currency on a requested currency name
        public void Calculate(DiskSize myDiskSize, decimal maxdisks, decimal mincapacity, decimal miniops, decimal minthroughput, TraceWriter log)
        {
            decimal DiskCountCapacity = Math.Ceiling(mincapacity / myDiskSize.MaxDataDiskSizeGB);
            decimal DiskCountIops = Math.Ceiling(miniops / myDiskSize.MaxDataDiskIops);
            decimal DiskCountThroughput = Math.Ceiling(minthroughput / myDiskSize.MaxDataDiskThroughputMBs);
            decimal DiskCountNeeded = Math.Max(DiskCountCapacity, DiskCountIops);
            DiskCountNeeded = Math.Max(DiskCountThroughput, DiskCountNeeded);
            decimal DiskConfigPrice = DiskCountNeeded * myDiskSize.Price;

            log.Info("DiskConfigPrice : " + DiskConfigPrice.ToString());
            log.Info("DiskPrice : " + DiskPrice.ToString());
            log.Info("DiskCountCapacity : " + DiskCountCapacity.ToString());
            log.Info("DiskCountIops : " + DiskCountIops.ToString());
            log.Info("DiskCountThroughput : " + DiskCountThroughput.ToString());
            log.Info("DiskCountNeeded : " + DiskCountNeeded.ToString());
            log.Info("MaxDisks : " + maxdisks.ToString());

            if (DiskConfigPrice < DiskPrice && DiskCountNeeded <= maxdisks)
            {
                DiskPrice = DiskConfigPrice;
                DiskCurrency = myDiskSize.Currency;
                DiskType = myDiskSize.Tier;
                DiskName = myDiskSize.Name;
                DiskSize = myDiskSize.Size;
                DiskCapacity = myDiskSize.MaxDataDiskSizeGB;
                DiskIops = myDiskSize.MaxDataDiskIops;
                DiskThroughput = myDiskSize.MaxDataDiskThroughputMBs;
                DiskCount = DiskCountNeeded;
                DiskConfigCapacity = myDiskSize.MaxDataDiskSizeGB * DiskCountNeeded;
                DiskConfigIops = myDiskSize.MaxDataDiskIops * DiskCountNeeded;
                DiskConfigThroughput = myDiskSize.MaxDataDiskThroughputMBs * DiskCountNeeded;
                DiskConfigDescription = "A striped (raid0) set of " + DiskCountNeeded + " " + myDiskSize.Size + " " + "disk(s).";
            }

        }
    }

    [BsonIgnoreExtraElements] // Ignore all non-declared objects
    public class DiskSize
    {
        /*
        {
	        "_id" : "md-p4-premium-asia-pacific-east",
	        "name" : "md-p4-premium-asia-pacific-east",
	        "type" : "disk",
	        "tier" : "premium",
	        "size" : "p4",
	        "region" : "asia-pacific-east",
	        "price" : 5.8072,
	        "MaxDataDiskSizeGB" : 32,
	        "MaxDataDiskIops" : 120,
	        "MaxDataDiskThroughputMBs" : 25,
	        "price_USD" : 5.8072,
	        "price_EUR" : 4.89721176,
	        "price_GBP" : 4.3281584248,
	        "price_AUD" : 7.396630640000001,
	        "price_JPY" : 592.3344,
	        "price_CAD" : 7.06097448,
	        "price_DKK" : 36.54703248,
	        "price_CHF" : 5.24448232,
	        "price_SEK" : 45.70963264,
	        "price_IDR" : 79326.352,
	        "price_INR" : 383.834143
        }
        */

        [Display(Description = "The name of the disk")]
        [BsonElement("name")]
        public string Name { get; set; }

        [Display(Description = "The name of the Tier")]
        [BsonElement("tier")]
        public string Tier { get; set; }

        [Display(Description = "The size of the disk")]
        [BsonElement("size")]
        public string Size { get; set; }

        [Display(Description = "The region linked to the pricing & availability of this VM Size")]
        [BsonElement("region")]
        public string Region { get; set; }

        [Display(Description = "Maximum amount of data possible")]
        [BsonElement("MaxDataDiskSizeGB")]
        public Decimal MaxDataDiskSizeGB { get; set; }

        [Display(Description = "Maximum amount of IOPS")]
        [BsonElement("MaxDataDiskIops")]
        public Decimal MaxDataDiskIops { get; set; }

        [Display(Description = "Maximum throughput (in MB/s)")]
        [BsonElement("MaxDataDiskThroughputMBs")]
        public Decimal MaxDataDiskThroughputMBs { get; set; }

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
        public void SetCurrency(string currency)
        {
            Currency = currency;
            Price = Convert.ToDecimal(this.GetType().GetProperty("price_" + currency).GetValue(this, null));
        }
    }

    public static class GetDiskConfig
    {
        [FunctionName("GetDiskConfig")]
        [Display(Name = "GetDiskConfig", Description = "Find the best data disk configuration for your given specifications")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "options", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            // CosmosDB Parameters, retrieved via environment variables
            string databaseName = Environment.GetEnvironmentVariable("cosmosdbDatabaseName");
            string collectionName = Environment.GetEnvironmentVariable("cosmosdbCollectionName");
            string mongodbConnectionString = Environment.GetEnvironmentVariable("cosmosdbMongodbConnectionString");

            // This endpoint is valid for all MongoDB
            var client = new MongoClient(mongodbConnectionString);
            var database = client.GetDatabase(databaseName);
            var collection = database.GetCollection<BsonDocument>(collectionName);

            // Region #
            string region = GetParameter("region", "europe-west", req).ToLower();
            log.Info("Region : " + region.ToString());
            // Currency #
            string currency = GetParameter("currency", "EUR", req).ToUpper();
            log.Info("Currency : " + currency.ToString());
            // Ssd #
            string disktype = GetParameter("disktype", "all", req).ToLower();
            string ssd = GetParameter("ssd", "all", req).ToLower();
            string[] ssdfilter = new string[3];
            ssdfilter = SSDFilter(disktype, ssd);
            log.Info("SSD : " + ssd.ToString());
            log.Info("Type : " + disktype.ToString());
            log.Info("SSD[0] : " + ssdfilter[0]);
            log.Info("SSD[1] : " + ssdfilter[1]);
            log.Info("SSD[2] : " + ssdfilter[2]);
            // IOPS (Min) #
            decimal iops = Convert.ToDecimal(GetParameter("iops", "1", req));
            iops = SetMinimum(iops, 1);
            log.Info("IOPS : " + iops.ToString());
            // Throughput (Min) #
            decimal throughput = Convert.ToDecimal(GetParameter("throughput", "1", req));
            throughput = SetMinimum(throughput, 1);
            log.Info("Throughput : " + throughput.ToString());
            // Data (Disk Capacity) (Min) #
            decimal data = Convert.ToDecimal(GetParameter("data", "1", req));
            data = SetMinimum(data, 1);
            log.Info("Data : " + data.ToString());
            // Max Disks (Min) #
            decimal maxdisks = Convert.ToDecimal(GetParameter("maxdisks", "64", req));
            maxdisks = SetMinimum(maxdisks, 1);
            log.Info("MaxDisks : " + maxdisks.ToString());

            var filterBuilder = Builders<BsonDocument>.Filter;
            var filter = filterBuilder.Eq("type", "disk")
                        & filterBuilder.Eq("disktype", "md")
                        & filterBuilder.Eq("region", region)
                        & filterBuilder.In("tier", ssdfilter)
                        ;
            var sort = Builders<BsonDocument>.Sort.Ascending("price");
            var cursor = collection.Find<BsonDocument>(filter).Sort(sort).ToCursor();

            // Load Application Insights
            string ApplicationInsightsKey = TelemetryConfiguration.Active.InstrumentationKey = System.Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY", EnvironmentVariableTarget.Process);
            TelemetryClient telemetry = new TelemetryClient() { InstrumentationKey = ApplicationInsightsKey };

            // Get results and put them into a list of objects
            DiskConfig myDiskConfig = new DiskConfig();
            myDiskConfig.DiskPrice = 999999999;
            foreach (var document in cursor.ToEnumerable())
            {   
                // Get RequestCharge
                var LastRequestStatistics = database.RunCommand<BsonDocument>(new BsonDocument { { "getLastRequestStatistics", 1 } });
                double RequestCharge = (double)LastRequestStatistics["RequestCharge"];
                telemetry.TrackMetric("RequestCharge", RequestCharge);
                
                // log.Info(document.ToString());
                DiskSize myDiskSize = BsonSerializer.Deserialize<DiskSize>(document);
                // log.Info(myDiskSize.Name);
                myDiskSize.SetCurrency(currency);
                myDiskConfig.Calculate(myDiskSize, maxdisks, data, iops, throughput, log);
            }

            // Convert to JSON & return it
            var json = JsonConvert.SerializeObject(myDiskConfig, Formatting.Indented);
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

        static public string[] SSDFilter(string disktype, string premium)
        {
            string[] response = new string[3];

            switch (disktype.ToLower())
            {
                case "standardhdd":
                    response[0] = "standardhdd";
                    response[1] = "standardhdd";
                    response[2] = "standardhdd";
                    break;
                case "standardssd":
                    response[0] = "standardssd";
                    response[1] = "standardssd";
                    response[2] = "standardssd";
                    break;
                case "premiumssd":
                    response[0] = "premiumssd";
                    response[1] = "premiumssd";
                    response[2] = "premiumssd";
                    break;
                default:
                    if (premium.ToLower() == "yes")
                    {
                        response[0] = "standardssd";
                        response[1] = "premiumssd";
                        response[2] = "premiumssd";
                        break;
                    }
                    if (premium.ToLower() == "no")
                    { 
                        response[0] = "standardhdd";
                        response[1] = "standardhdd";
                        response[2] = "standardhdd";
                        break;
                    }
                    response[0] = "standardssd";
                    response[1] = "premiumssd";
                    response[2] = "standardhdd";
                    break;
            }

            return response;
        }

    }
}
