using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

using System.Linq;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace vmchooser
{
    public static class RescaleCosmosDB
    {
        [Disable]
        [FunctionName("RescaleCosmosDB")]
        public static void Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info("Let's get this CosmosDB Collection rescaled! " + DateTime.Now.ToString());

            Decimal minRequestUnits = 400;
            Decimal requestunits = minRequestUnits;

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
            string collectionRid = collection.GetPropertyValue<string>("_rid");

            // Loop through offers
            var offersFeed = client.CreateOfferQuery().AsEnumerable().ToArray();
            if (offersFeed != null)
            {
                foreach (var offer in offersFeed)
                {
                    var offerColl = client.ReadDocumentCollectionAsync(offer.ResourceLink);
                    string offerCollectionRid = offerColl.Result.Resource.GetPropertyValue<string>("_rid");

                    // Change matching offer to newly requested Request Units
                    if (offerCollectionRid == collectionRid)
                    {
                        Offer newOffer = client.CreateOfferQuery()
                                        .Where(r => r.ResourceLink == collection.SelfLink)
                                        .AsEnumerable()
                                        .SingleOrDefault();
                        newOffer = new OfferV2(newOffer, Convert.ToInt16(requestunits));
                        client.ReplaceOfferAsync(newOffer);
                        log.Info("Reset request units to " + requestunits.ToString() + "RU");
                    }
                }
            }
        }
    }
}
