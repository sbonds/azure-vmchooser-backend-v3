using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;

namespace vmchooser
{
    public static class ScaleCosmosDB
    {
        [Disable]
        [FunctionName("ScaleCosmosDB")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("Let's get this CosmosDB Collection rescaled!");

            // parse query parameter&&1é1
            string ru = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => string.Compare(q.Key, "ru", true) == 0)
                .Value;

            // Get request body
            dynamic data = await req.Content.ReadAsAsync<object>();

            // Set name to query string or body data
            ru = ru ?? data?.ru;
            log.Info("Requested " + ru + " RU by API call");

            Decimal requestunits = 400;
            if (Decimal.TryParse(ru, out requestunits))
            {
                // Ensure that the request units are dividable by 100
                requestunits = Math.Ceiling(requestunits / 100) * 100;
                log.Info("Trying to change collection to " + requestunits.ToString() + " RU");
            }

            Decimal minRequestUnits = 400;
            Decimal maxRequestUnits = 10000;

            // Validate if the request units are within variable parameters.
            if (requestunits >= minRequestUnits && requestunits <= maxRequestUnits)
            {

                // CosmosDB Parameters, retrieved via environment variables
                string host = Environment.GetEnvironmentVariable("cosmosdbHostName");
                string password = Environment.GetEnvironmentVariable("cosmosdbPassword");
                string databaseName = Environment.GetEnvironmentVariable("cosmosdbDatabaseName");
                string collectionName = Environment.GetEnvironmentVariable("cosmosdbCollectionName");

                // This endpoint is valid for all APIs (tested on DocDB/SQL/MongoDB/...)
                string endpoint = string.Format("https://{0}:443/", host);
                Uri endpointUri = new Uri(endpoint);
                DocumentClient client = new DocumentClient(endpointUri, password);

                // Find links to DB Account & Collection in order to match the Offer
                Database database = client.CreateDatabaseQuery().Where(d => d.Id == databaseName).AsEnumerable().FirstOrDefault();
                string databaseLink = database.SelfLink;
                DocumentCollection collection = client.CreateDocumentCollectionQuery(databaseLink).Where(c => c.Id == collectionName).AsEnumerable().FirstOrDefault();
                string collectionLink = collection.SelfLink;
                //log.Info(collection.ToString());
                string collectionRid = collection.GetPropertyValue<string>("_rid");
                //log.Info(collectionRid);

                // Loop through offers
                var offersFeed = client.CreateOfferQuery().AsEnumerable().ToArray();
                if (offersFeed != null)
                {
                    foreach (var offer in offersFeed)
                    {
                        var offerColl = await client.ReadDocumentCollectionAsync(offer.ResourceLink);
                        //log.Info(offerColl.Resource.ToString());
                        string offerCollectionRid = offerColl.Resource.GetPropertyValue<string>("_rid");
                        //log.Info(offerCollectionRid);

                        // Change matching offer to newly requested Request Units
                        if (offerCollectionRid == collectionRid)
                        {
                            Offer newOffer = client.CreateOfferQuery()
                                            .Where(r => r.ResourceLink == collection.SelfLink)
                                            .AsEnumerable()
                                            .SingleOrDefault();
                            newOffer = new OfferV2(newOffer, Convert.ToInt16(requestunits));
                            await client.ReplaceOfferAsync(newOffer);
                            log.Info("Changed request units to " + requestunits.ToString() + "RU");
                            return req.CreateResponse(HttpStatusCode.OK, "Changed request units to " + requestunits.ToString());
                        }
                    }
                } else {
                    return req.CreateResponse(HttpStatusCode.BadRequest, "No offers found in offerfeed");
                }
            } else {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a ru between" + minRequestUnits + " and " + maxRequestUnits + "!");
            }
            return req.CreateResponse(HttpStatusCode.BadRequest, "No offers found for the requested collection");
        }
    }
}
