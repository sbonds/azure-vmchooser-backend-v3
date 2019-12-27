using System.IO;
using System.Net;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;

namespace vmchooser
{
    public static class ValidateCsvFile
    {
        [Disable]
        [FunctionName("ValidateCsvFile")]
        public static void Run([BlobTrigger("input/{name}", Connection = "vmchooser-sa-blob-input")]string myBlob, string name, TraceWriter log)
        {
            log.Info($"VMchooser Bulk Mapping \n Name:{name} \n Size: {myBlob.Length} Bytes");

            int expected_field_count = System.Int32.Parse(System.Environment.GetEnvironmentVariable("vmchooser-csv-fieldcount"));

            using (StringReader reader = new StringReader(myBlob))
            {
                string line;
                int msgcount = 0;
                int linecount = 0;
                string vmchooser_sa_queue_batch = System.Environment.GetEnvironmentVariable("vmchooser-sa-queue-batch");
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(vmchooser_sa_queue_batch);
                CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
                CloudQueue queue = queueClient.GetQueueReference("vmchooserbatch");
                queue.CreateIfNotExists();
                while ((line = reader.ReadLine()) != null)
                {
                    char delimiter = ',';
                    string[] fields = line.Split(delimiter);
                    int field_count = fields.Length;
                    string field_count_str = field_count.ToString();
                    if (field_count == expected_field_count)
                    {
                        if (linecount > 0)
                        {
                            line = line + delimiter + name;
                            CloudQueueMessage message = new CloudQueueMessage(line);
                            queue.AddMessageAsync(message);
                            log.Info("Message added to the queue");
                            msgcount++;
                        } else {
                            log.Info("Header row ignored"); //Ignore first line as this is the header
                        }
                        linecount++;
                    }
                    else
                    {
                        log.Error("Wrong amount of fields "+field_count_str+" received. Was "+delimiter+" used as a delimiter?");
                    }
                }
                string vmchooser_api_scalecosmosdb = System.Environment.GetEnvironmentVariable("vmchooser-api-scalecosmosdb");
                int ru = msgcount * 30;
                int minru = 400;
                int maxru = 10000;
                if (ru < minru) { ru = minru; }
                if (ru > maxru) { ru = maxru; }
                string apicall = vmchooser_api_scalecosmosdb + "&ru=" + ru.ToString();
                HttpWebRequest request = WebRequest.Create(apicall) as HttpWebRequest;
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            }
        }
    }
}
