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
    public class CloudSimple
    {
        /*
        */

        [Display(Description = "The name of the cloudsimple instnace")]
        [BsonElement("name")]
        public string Name { get; set; }

        [Display(Description = "The region linked to the pricing & availability of this cloud instnace")]
        [BsonElement("region")]
        public string Region { get; set; }

        [Display(Description = "The contract linked to the pricing & availability of this cloud insance")]
        [BsonElement("Contract")]
        public string Dimension { get; set; }

        [Display(Description = "The numbers of cores of this cloud instance")]
        [BsonElement("cores")]
        [BsonRepresentation(BsonType.Decimal128, AllowTruncation = true)]
        public Decimal Cores { get; set; }

        [Display(Description = "The size of the memory of this cloud instance")]
        [BsonElement("ram")]
        [BsonRepresentation(BsonType.Decimal128, AllowTruncation = true)]
        public Decimal Ram { get; set; }

        [Display(Description = "The size of the uncached storage of this cloud instance")]
        [BsonElement("storage")]
        [BsonRepresentation(BsonType.Decimal128, AllowTruncation = true)]
        public Decimal Storage { get; set; }

        [Display(Description = "The size of the storage cache of this cloud instance")]
        [BsonElement("cache")]
        [BsonRepresentation(BsonType.Decimal128, AllowTruncation = true)]
        public Decimal Cache { get; set; }

        [Display(Description = "The price per hour for this cloud instance expressed in the indicated currency")]
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

        // Calculate the deployment size
        public dynamic calculateDeploymentSize(ILogger log, decimal cores, decimal memory, decimal storage, decimal overcommit, decimal deduplication, decimal ftt, decimal ftm) {
            // Add FTM to storage
            double ftmfactor = 1;
            switch (ftm)
            {
                case 1:
                    // 1+1
                    ftmfactor = 0.5;
                    break;
                case 5:
                    // 3+1
                    ftmfactor = 0.75;
                    break;
                case 6:
                    // 4+2
                    ftmfactor = 0.66;
                    break;
                default:
                    break;
            }
            double doublestorage = (double)storage / ftmfactor;
            doublestorage /= (double) deduplication;

            double doublecores = (double) cores / (double) overcommit;

            // Get the amount of nodes needed to cover the needs per dimension
            Decimal corenodes = Math.Ceiling( (decimal) doublecores / this.Cores);
            Decimal memorynodes = Math.Ceiling(memory / this.Ram);
            Decimal storagenodes = Math.Ceiling( (decimal) doublestorage / this.Storage);

            // Get the highest node count of the three dimensions ; cores, memory & storage
            Decimal needednodes = Math.Max(corenodes, memorynodes);
            needednodes = Math.Max(needednodes, storagenodes);
            // Add FTT to node count
            needednodes += ftt;
            // AVS needs a minimum of 3 nodes
            needednodes = Math.Max(needednodes, 3);

            decimal HoursPerMonth = 730;

            // Return results
            dynamic results = new System.Dynamic.ExpandoObject();
            results.TotalPricePerHour = needednodes * this.Price / HoursPerMonth;
            results.TotalPricePerMonth = needednodes * this.Price;
            results.TotalNodes = needednodes;
            results.TotalNodesUsable = (needednodes - ftt);
            results.TotalCores = needednodes * this.Cores;
            results.TotalCoresUsable = (needednodes - ftt) * this.Cores;
            results.TotalCoresOvercommitUsable = (needednodes - ftt) * this.Cores * overcommit;
            results.TotalMemoryGB = needednodes * this.Ram;
            results.TotalMemoryGBUsable = (needednodes - ftt) * this.Ram;
            results.TotalStorageRawTB = needednodes * this.Storage;
            results.TotalStorageRawTBUsable = (needednodes - ftt) * this.Storage;
            results.TotalStorageRawTBDedupUsable = (needednodes - ftt) * this.Storage * deduplication;
            results.TotalStorageUsableTB = (decimal) ( 
                                                (double) needednodes * 
                                                ( (double) this.Storage * ftmfactor) 
                                           );
            results.TotalStorageUsableTBUsable = (decimal) ( 
                                                    ( (double) (needednodes - ftt) * 
                                                    ( (double) this.Storage * ftmfactor) ) 
                                                 ) ;
            results.TotalStorageUsableTBDedupUsable = (decimal) (
                                        ( (double) (needednodes - ftt) *
                                        ( (double) this.Storage * ftmfactor) ) *
                                        (double) deduplication
                                     );
            results.NodeType = this.Name;
            results.NodeCores = this.Cores;
            results.NodeMemoryGB = this.Ram;
            results.NodeStorageRawTB = this.Storage;
            results.NodePricePerHour = this.Price;
            results.NodePricePerMonth = this.Price * HoursPerMonth;
            return results;
        }

    }

    public static class GetCloudSimple
    {
        [FunctionName("GetCloudSimple")]
        [Display(Name = "GetCloudSimple", Description = "Get the cost for a Azure CloudSimple deployment given a set of specifications")]
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

            // Cores
            decimal cores = Convert.ToDecimal(GetParameter("cores", "0", req));
            cores = SetMinimum(cores, 0);
            log.LogInformation("Cores : " + cores.ToString());
            // Memory
            decimal memory = Convert.ToDecimal(GetParameter("memory", "0", req));
            memory = SetMinimum(memory, 0);
            log.LogInformation("Memory : " + memory.ToString());
            // Storage
            decimal storage = Convert.ToDecimal(GetParameter("storage", "0", req));
            storage = SetMinimum(storage, 0);
            log.LogInformation("Storage : " + storage.ToString());
            // Overcommit
            decimal overcommit = Convert.ToDecimal(GetParameter("overcommit", "1", req));
            overcommit = SetMinimum(overcommit, 0);
            log.LogInformation("Overcommit : " + overcommit.ToString());
            // Deduplication
            decimal deduplication = Convert.ToDecimal(GetParameter("deduplication", "1", req));
            deduplication = SetMinimum(deduplication, 0);
            log.LogInformation("Deduplication : " + deduplication.ToString());
            // FTT - Failures To Tolerate
            decimal ftt = Convert.ToDecimal(GetParameter("ftt", "0" +
                "", req));
            ftt = SetMinimum(ftt, 0);
            log.LogInformation("FTT : " + ftt.ToString());
            // FTM - Fault Tolerance Mode
            decimal ftm = Convert.ToDecimal(GetParameter("ftm", "0", req));
            ftm = SetMinimum(ftm, 0);
            log.LogInformation("FTM : " + ftm.ToString());
            // Contract
            string contract = GetParameter("contract", "payg", req).ToLower();
            log.LogInformation("Contract : " + contract.ToString());
            // Region #
            string region = GetParameter("region", "europe-west", req).ToLower();
            log.LogInformation("Region : " + region.ToString());
            // Currency #
            string currency = GetParameter("currency", "EUR", req).ToUpper();
            log.LogInformation("Currency : " + currency.ToString());

            // Create Return Object
            dynamic documents = new System.Dynamic.ExpandoObject();
            dynamic results = new System.Dynamic.ExpandoObject();

            // Get Backup Instance Cost
            var filterBuilder = Builders<BsonDocument>.Filter;
            var filter = filterBuilder.Eq("type", "avs")
                        & filterBuilder.Eq("contract", contract)
                        & filterBuilder.Eq("region", region)
                        ;
            var sort = Builders<BsonDocument>.Sort.Ascending("price");
            var cursor = collection.Find<BsonDocument>(filter).Sort(sort).ToCursor();
            decimal costwatermark = decimal.MaxValue;
            foreach (var document in cursor.ToEnumerable())
            {
                // Get RequestCharge
                var LastRequestStatistics = database.RunCommand<BsonDocument>(new BsonDocument { { "getLastRequestStatistics", 1 } });
                double RequestCharge = (double)LastRequestStatistics["RequestCharge"];
                log.LogMetric("RequestCharge", RequestCharge);

                // Get Document
                log.LogInformation(document.ToString());
                CloudSimple myCloudSimple = BsonSerializer.Deserialize<CloudSimple>(document);
                myCloudSimple.setCurrency(currency);
                results = myCloudSimple.calculateDeploymentSize(log, cores, memory, storage, overcommit, deduplication, ftt, ftm);
                if (results.TotalPricePerMonth < costwatermark) {
                    documents = results;
                }
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
