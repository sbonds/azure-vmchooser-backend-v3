using System;
using System.IO;
using System.Net;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json.Linq;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace vmchooser
{
    public class VmchooserEntity : TableEntity
    {
        public VmchooserEntity(string csvfile, string vmname)
        {
            this.PartitionKey = csvfile;
            this.RowKey = vmname;
        }

        public VmchooserEntity() { }

        public string InputName { get; set; }
        public string InputRegion { get; set; }
        public string InputCores { get; set; }
        public string InputMemory { get; set; }
        public string InputSSD { get; set; }
        public string InputNICS { get; set; }
        public string InputData { get; set; }
        public string InputIOPS { get; set; }
        public string InputThroughput { get; set; }
        public string InputTemp { get; set; }
        public string InputAvgCPU { get; set; }
        public string InputAvgMEM { get; set; }
        public string InputContract { get; set; }
        public string InputCurrency { get; set; }
        public string InputBurstable { get; set; }
        public string Name { get; set; }
        public string ACU { get; set; }
        public string SSD { get; set; }
        public string Cores { get; set; }
        public string PCores { get; set; }
        public string MemoryGB { get; set; }
        public string NICS { get; set; }
        public string BandwidthMbps { get; set; }
        public string MaxDisks { get; set; }
        public string MaxIOPS { get; set; }
        public string MaxThroughputMBs { get; set; }
        public string PriceHour { get; set; }
        public string Price200h { get; set; }
        public string PriceMonth { get; set; }
        public string DiskType { get; set; }
        public string DiskConfig { get; set; }
        public string DiskConfigPrice { get; set; }
    }
    public static class ParseCsvQueue
    {
        [Disable]
        [FunctionName("ParseCsvQueue")]
        public static void Run([QueueTrigger("vmchooserbatch", Connection = "vmchooser-sa-queue-batch")]string csvQueueItem, TraceWriter log)
        {
            string separator = ",";
            string[] values = System.Text.RegularExpressions.Regex.Split(csvQueueItem, separator); // Hard format, column based... :x
            string vmname = values[0];
            string region = values[1];
            string cores = values[2];
            string memory = values[3];
            string ssd = values[4];
            string nic = values[5];
            string data = values[6];
            string iops = values[7];
            string throughput = values[8];
            string temp = values[9];
            string peakcpu = values[10];
            string peakmem = values[11];
            string currency = values[12];
            string contract = values[13];
            string burst = values[14];
            string csvfile = values[15];

            // Fix for "dynamic" pricing mapping later on with the results
            currency = currency.ToUpper();
            
            // Retrieve most optimal vm size
            string vmchooser_api_authorizationkey = System.Environment.GetEnvironmentVariable("vmchooser-api-authorizationkey");
            string vmchooser_api_url_getvmsize = System.Environment.GetEnvironmentVariable("vmchooser-api-url-getvmsize");
            string querysuffix = "?burstable="+burst+ "&maxresults=1&region=" + region + "&cores=" + cores + "&memory=" + memory + "&iops=" + iops + "&data=" + data + "&temp=" + temp + "&throughput=" + throughput + "&nics=" + nic + "&ssd=" + ssd + "&avgcpupeak=" + peakcpu + "&avgmempeak=" + peakmem + "&currency=" + currency + "&contract=" + contract;
            string apicall = vmchooser_api_url_getvmsize + querysuffix;
            //log.Info(apicall);

            HttpWebRequest request = WebRequest.Create(apicall) as HttpWebRequest;
            request.Headers["Ocp-Apim-Subscription-Key"] = vmchooser_api_authorizationkey;
            request.Method = "POST";
            request.ContentLength = 0;

            try
            {
                using (WebResponse response = request.GetResponse())
                {
                    using (Stream stream = response.GetResponseStream())
                    {
                        StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8);
                        String stringResponse = reader.ReadToEnd();
                        /* Example JSON Return
                        {
                           "1":{
                              "Name":"b4ms",
                              "Region":"europe-north",
                              "Contract":"ri1y",
                              "Price (GBP/Hour)":0.098693817780000012,
                              "Price (GBP/200h)":19.738763556000002,
                              "Price (GBP/Month)":73.428200428320011,
                              "ACU":-1,
                              "SSD":"No",
                              "Cores":4,
                              "pCores":0.14625000000000002,
                              "Memory (GB)":16,
                              "NICs":4,
                              "Bandwidth (Mbps)":-1,
                              "Max Disks":8,
                              "Max IOPS":2880,
                              "Max Throughput (MB/s)":35
                           }
                        }
                        */
                        JObject joResponse = JObject.Parse(stringResponse);
                        dynamic vmInfo = (JObject)joResponse["1"];
                        if (vmInfo == null)
                        {
                            log.Info("No results recevied");
                            return;
                        }
                        else
                        {
                            string vmInfoName = vmInfo.Name;
                            string vmInfoRegion = vmInfo.Region;
                            string vmInfoContract = vmInfo.Contract;
                            string vmInfoPricehour = vmInfo["Price (" + currency + "/Hour)"];
                            string vmInfoPrice200h = vmInfo["Price (" + currency + "/200h)"];
                            string vmInfoPricemonth = vmInfo["Price (" + currency + "/Month)"];
                            string vmInfoAcu = vmInfo.ACU;
                            string vmInfoSsd = vmInfo.SSD;
                            string vmInfoCores = vmInfo.Cores;
                            string vmInfoPcores = vmInfo.pCores;
                            string vmInfoMemory = vmInfo["Memory (GB)"];
                            string vmInfoNics = vmInfo.NICs;
                            string vmInfoBandwidth = vmInfo["Bandwidth (Mbps)"];
                            string vmInfoDisks = vmInfo["Max Disks"];
                            string vmInfoIops = vmInfo["Max IOPS"];
                            string vmInfoThroughput = vmInfo["Max Throughput (MB/s)"];
                            //log.Info(vmInfoName + "-" + vmInfoRegion + "-" + vmInfoContract + "-" + vmInfoPricehour + "-" + vmInfoPrice200h + "-" + vmInfoPricemonth + "-" + vmInfoAcu + "-" + vmInfoSsd + "-" + vmInfoCores + "-" + vmInfoPcores + "-" + vmInfoMemory + "-" + vmInfoNics + "-" + vmInfoBandwidth + "-" + vmInfoDisks + "-" + vmInfoIops + "-" + vmInfoThroughput);

                            // Retrieve most optimal disk config
                            string vmchooser_api_url_getdisksize = System.Environment.GetEnvironmentVariable("vmchooser-api-url-getdisksize");
                            decimal fixeddata;
                            bool parsed = Decimal.TryParse(data, out fixeddata);
                            //log.Info("FixedData:" + fixeddata.ToString() + " - Data:" + data);
                            fixeddata = fixeddata * 1024; //Convert TB to GB
                            //log.Info("FixedData:" + fixeddata.ToString() + " - Data:" + data);
                            string diskquerysuffix = "?region=" + region + "&iops=" + iops + "&data=" + fixeddata.ToString() + "&throughput=" + throughput + "&currency=" + currency + "&ssd=" + ssd + "&maxdisks=" + vmInfoDisks;
                            string diskapicall = vmchooser_api_url_getdisksize + diskquerysuffix;
                            //log.Info(diskapicall);

                            HttpWebRequest diskrequest = WebRequest.Create(diskapicall) as HttpWebRequest;
                            diskrequest.Headers["Ocp-Apim-Subscription-Key"] = vmchooser_api_authorizationkey;
                            diskrequest.Method = "POST";
                            diskrequest.ContentLength = 0;
                            try
                            {
                                using (WebResponse diskresponse = diskrequest.GetResponse())
                                {
                                    using (Stream diskstream = diskresponse.GetResponseStream())
                                    {
                                        StreamReader diskreader = new StreamReader(diskstream, System.Text.Encoding.UTF8);
                                        String stringDiskResponse = diskreader.ReadToEnd();
                                        log.Info("Disk : " + stringDiskResponse);
                                        /* Example JSON Return 
                                        {
                                           "Disk T-Shirt Size":"s20",
                                           "Disk Type":"standard",
                                           "Capacity (GB) - per disk":512,
                                           "IOPS (IO/s) - per disk":500,
                                           "Througput (MB/s) - per disk":60,
                                           "Number of Disks":3,
                                           "Capacity (GB) - for all disks":1536,
                                           "IOPS (IO/s) - for all disks":1500,
                                           "Througput (MB/s) - for all disks":180,
                                           "Description":"A raid0 / stripe of 3 disks of type s20",
                                           "Price / Month - for all disks":55.050624000000006,
                                           "Currency":"EUR"
                                        }
                                        */
                                        JObject joDiskResponse = JObject.Parse(stringDiskResponse);
                                        dynamic diskInfo = (JObject)joDiskResponse;
                                        string diskInfoTshirtsize = diskInfo["Disk T-Shirt Size"];
                                        string diskInfoType = diskInfo["Disk Type"];
                                        string diskInfoCapacitySingle = diskInfo["Capacity (GB) - per disk"];
                                        string diskInfoIopsSingle = diskInfo["IOPS (IO/s) - per disk"];
                                        string diskInfoThoughputSingle = diskInfo["Througput (MB/s) - per disk"];
                                        string diskInfoDiskcount = diskInfo["Number of Disks"];
                                        string diskInfoCapacityTotal = diskInfo["Capacity (GB) - for all disks"];
                                        string diskInfoIopsTotal = diskInfo["IOPS (IO/s) - for all disks"];
                                        string diskInfoThroughputTotal = diskInfo["Througput (MB/s) - for all disk"];
                                        string diskInfoDescription = diskInfo["Description"];
                                        string diskInfoPrice = diskInfo["Price / Month - for all disks"];
                                        string diskInfoCurrency = diskInfo["Currency"];

                                        // Write Results to Table Storage
                                        string vmchooser_sa_table_batch = System.Environment.GetEnvironmentVariable("vmchooser-sa-table-batch");
                                        CloudStorageAccount storageAccount = CloudStorageAccount.Parse(vmchooser_sa_table_batch);
                                        CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                                        CloudTable table = tableClient.GetTableReference("vmchooserbatch");
                                        table.CreateIfNotExists();
                                        VmchooserEntity vmresult = new VmchooserEntity(csvfile, vmname);
                                        vmresult.InputName = vmname;
                                        vmresult.InputRegion = region;
                                        vmresult.InputCores = cores;
                                        vmresult.InputMemory = memory;
                                        vmresult.InputSSD = ssd;
                                        vmresult.InputNICS = nic;
                                        vmresult.InputData = data;
                                        vmresult.InputIOPS = iops;
                                        vmresult.InputThroughput = throughput;
                                        vmresult.InputTemp = temp;
                                        vmresult.InputAvgCPU = peakcpu;
                                        vmresult.InputAvgMEM = peakmem;
                                        vmresult.InputContract = contract;
                                        vmresult.InputCurrency = currency;
                                        vmresult.InputBurstable = burst;
                                        vmresult.DiskType = diskInfoType;
                                        vmresult.DiskConfig = diskInfoDescription;
                                        vmresult.DiskConfigPrice = diskInfoPrice;
                                        vmresult.Name = vmInfoName;
                                        vmresult.ACU = vmInfoAcu;
                                        vmresult.SSD = vmInfoSsd;
                                        vmresult.Cores = vmInfoCores;
                                        vmresult.PCores = vmInfoPcores;
                                        vmresult.MemoryGB = vmInfoMemory;
                                        vmresult.NICS = vmInfoNics;
                                        vmresult.BandwidthMbps = vmInfoBandwidth;
                                        vmresult.MaxDisks = vmInfoDisks;
                                        vmresult.MaxIOPS = vmInfoIops;
                                        vmresult.MaxThroughputMBs = vmInfoThroughput;
                                        vmresult.PriceHour = vmInfoPricehour;
                                        vmresult.Price200h = vmInfoPrice200h;
                                        vmresult.PriceMonth = vmInfoPricemonth;
                                        TableOperation insertOperation = TableOperation.Insert(vmresult);
                                        table.Execute(insertOperation);
                                    }
                                }
                            }
                            catch (WebException e)
                            {
                                using (WebResponse diskresponse = e.Response)
                                {
                                    // Error whilst getting the Disk Sizing Info
                                    HttpWebResponse httpResponse = (HttpWebResponse)diskresponse;
                                    log.Error("Error code: " + httpResponse.StatusCode);
                                    return;
                                }
                            }
                        }
                    }
                }
            }
            catch (WebException e)
            {
                using (WebResponse response = e.Response)
                {
                    // Error whilst getting the VM Size Info
                    HttpWebResponse httpResponse = (HttpWebResponse)response;
                    log.Error("Error code: " + httpResponse.StatusCode);
                    return;
                }
            }
        }
    }
}
