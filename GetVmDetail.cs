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

namespace vmchooser
{

    public static class GetVmDetail
    {
        [FunctionName("GetVmDetail")]
        [ResponseType(typeof(VmSize))]
        [Display(Name = "GetVmDetail", Description = "Get the details for a specific VM T-Shirt size")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "options", Route = null)]HttpRequestMessage req, TraceWriter log)
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

            // Get Parameters
            dynamic contentdata = await req.Content.ReadAsAsync<object>();
            // Tier #
            string tier = GetParameter("tier", "standard", req).ToLower();
            log.Info("Tier : " + tier.ToString());
            // Region #
            string region = GetParameter("region", "europe-west", req).ToLower();
            log.Info("Region : " + region.ToString());
            // Currency #
            string currency = GetParameter("currency", "EUR", req).ToUpper();
            log.Info("Currency : " + currency.ToString());
            
            // Name
            string vmsize = GetParameter("vmsize", "a0", req).ToLower();
            log.Info("Name : " + vmsize.ToString());

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
                log.Info(document.ToString());
                VmSize myVmSize = BsonSerializer.Deserialize<VmSize>(document);
                log.Info(myVmSize.OperatingSystem);
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
    }
}
