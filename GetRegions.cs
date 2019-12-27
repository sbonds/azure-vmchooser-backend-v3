using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;

using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;

using Newtonsoft.Json;

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

namespace vmchooser
{
    [BsonIgnoreExtraElements] // Ignore all non-declared objects
    public class RegionList
    {
        [Display(Description = "The full name of the VM Size")]
        [BsonElement("name")]
        public string Name { get; set; }

        [Display(Description = "The system friendly name of the VM Size")]
        [BsonElement("slug")]
        public string Slug { get; set; }
    }

    public static class GetRegions
    {
        [FunctionName("GetRegions")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            // CosmosDB Parameters, retrieved via environment variables
            string databaseName = Environment.GetEnvironmentVariable("cosmosdbDatabaseName");
            string collectionName = Environment.GetEnvironmentVariable("cosmosdbCollectionName");
            string mongodbConnectionString = Environment.GetEnvironmentVariable("cosmosdbMongodbConnectionString");

            // This endpoint is valid for all MongoDB
            var client = new MongoClient(mongodbConnectionString);
            var database = client.GetDatabase(databaseName);
            var collection = database.GetCollection<BsonDocument>(collectionName);

            // Get all VM sizes
            var filterBuilder = Builders<BsonDocument>.Filter;
            var filter = filterBuilder.Eq("type", "region")
                        ;

            var cursor = collection.Find<BsonDocument>(filter).ToCursor();

            // Load Application Insights
            string ApplicationInsightsKey = TelemetryConfiguration.Active.InstrumentationKey = System.Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY", EnvironmentVariableTarget.Process);
            TelemetryClient telemetry = new TelemetryClient() { InstrumentationKey = ApplicationInsightsKey };

            // Get results and put them into a list of objects
            List<RegionList> documents = new List<RegionList>();
            foreach (var document in cursor.ToEnumerable())
            {
                // Get RequestCharge
                var LastRequestStatistics = database.RunCommand<BsonDocument>(new BsonDocument { { "getLastRequestStatistics", 1 } });
                double RequestCharge = (double)LastRequestStatistics["RequestCharge"];
                telemetry.TrackMetric("RequestCharge", RequestCharge);

                // Get Document
                log.Info(document.ToString());
                RegionList myRegion = BsonSerializer.Deserialize<RegionList>(document);
                documents.Add(myRegion);
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
