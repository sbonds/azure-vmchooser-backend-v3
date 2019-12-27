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
using Newtonsoft.Json;
using System.Text;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

namespace vmchooser
{

    public static class GetVmDetail
    {
        [FunctionName("GetVmDetail")]
        [Display(Name = "GetVmDetail", Description = "Get the details for a specific VM T-Shirt size")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "options", Route = null)]HttpRequest req, ILogger log)
        {
            // CosmosDB Parameters, retrieved via environment variables
            string databaseName = Environment.GetEnvironmentVariable("cosmosdbDatabaseName");
            string collectionName = Environment.GetEnvironmentVariable("cosmosdbCollectionName");
            string mongodbConnectionString = Environment.GetEnvironmentVariable("cosmosdbMongodbConnectionString");

            // Set BSON AutoMap
            //BsonClassMap.RegisterClassMap<VmSize>();

            // This endpoint is valid for all MongoDB
            var client = new MongoClient(mongodbConnectionString);
            var database = client.GetDatabase(databaseName);
            var collection = database.GetCollection<BsonDocument>(collectionName);

            // Tier #
            string tier = GetParameter("tier", "standard", req).ToLower();
            log.LogInformation("Tier : " + tier.ToString());
            // Region #
            string region = GetParameter("region", "europe-west", req).ToLower();
            log.LogInformation("Region : " + region.ToString());
            // Currency #
            string currency = GetParameter("currency", "EUR", req).ToUpper();
            log.LogInformation("Currency : " + currency.ToString());
            
            // Name
            string vmsize = GetParameter("vmsize", "a0", req).ToLower();
            log.LogInformation("Name : " + vmsize.ToString());

            // Get price for Linux
            var filterBuilder = Builders<BsonDocument>.Filter;
            var filter = filterBuilder.Eq("type", "vm")
                        & filterBuilder.Eq("region", region)
                        & filterBuilder.Eq("tier", tier)
                        & filterBuilder.Eq("name", vmsize)
                        ;

            var cursor = collection.Find<BsonDocument>(filter).ToCursor();

            // Get results and put them into a list of objects
            List<VmSize> documents = new List<VmSize>();
            foreach (var document in cursor.ToEnumerable())
            {
                log.LogInformation(document.ToString());
                VmSize myVmSize = BsonSerializer.Deserialize<VmSize>(document);
                log.LogInformation(myVmSize.OperatingSystem);
                myVmSize.setCurrency(currency);
                documents.Add(myVmSize);
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
    }
}
