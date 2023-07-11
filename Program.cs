using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Threading.Tasks;

namespace CustomReporting
{
    internal class Program
    {
        private static HttpClient client = new HttpClient();

        static async Task Main(string[] args)
        {
            Console.Write("Identify the URL for the Dynamics Marketing environment and note that as EnvironmentURL" +
                "\r\nAccess the https://<EnvironmentURL>/api/data/v9.1/datalakefolders URL\r\n" +
                "In the resulting JSON, search for data lake folder with name msdynmkt_aria_exporter_folder " +
                "and then:\r\nLocate the Container URL property, indicated by containerendpoint " +
                "and make note of the value as source ContainerURL: https://<storageaccountname>.dfs.core.windows.net/<containername>\r\n" +
                "Locate the path property and make a note of the GUID provided as source PathURL\r\n");

            //  Console.WriteLine("Enter containerURL - https://<storageaccountname>.dfs.core.windows.net/<containername>:");
            string sourceContainerUri = @"https://aethcccb15fe898e4edbb886.dfs.core.windows.net/aeth-8162bda5-b803-40c4-b5b8-d81ca1605f21";
            //Console.ReadLine();

            // Console.WriteLine("Enter pathURL - path of msdynmkt_aria_exporter_folder:");
            string sourcePath = @"20ec6262-b9cf-40f6-bafb-f3fba918d617";
            //Console.ReadLine();

            Console.WriteLine("Enter destinnation storage account name - samplestorageaccountname:https://sath01seaudtp1.blob.core.windows.net");
            string? destStorageAccountName = $"sath01seaudtp1";
            //Console.ReadLine();

            Console.WriteLine("Enter destination blob container name - samplecontainername:rtm-interaction");
            string? destBlobContainerName = $"rtm-interaction";

            string containerPathURL = string.Concat(sourceContainerUri, "/", sourcePath, "/");
            Console.Write("Source container path url is:" + containerPathURL);

            // Construct requestUri to call dataverse API
            string requestUri =
                string.Format("RetrieveAnalyticsStoreAccess(Url=@a,ResourceType='Folder',Permissions='Read,Write,List')?@a='{0}'", containerPathURL);

            //Get configuration data from App.config connectionStrings
            string connectionString = ConfigurationManager.ConnectionStrings["Connect"].ConnectionString;
            var expiresOn = DateTime.MinValue;
            // Get SAS token to list all blobs in the given directory by making a REST api call to:  GET: https://<<org URL>>/api/data/v9.1/RetrieveAnalyticsStoreAccess(Url=@a,ResourceType='File/Folder',Permissions='Read,Write')?@a='file/folder Path'
            client = HttpClientHelpers.GetHttpClient(connectionString, HttpClientHelpers.clientId, out expiresOn, HttpClientHelpers.redirectUrl);
            string sasToken = await DatalakeAPIHelper.GetSasTokenAsync(client, requestUri);

            ApplicationLog.Info($"SAS Token - {sasToken} --- RequestURI -- {requestUri}");

            // List all the blobs under source directory
            List<string> blobPaths = DatalakeAPIHelper.ListBlobsWithSas(containerPathURL, sasToken);

            ApplicationLog.Info($"Total BlobPaths - {blobPaths.Count}");
            // Construct the blob endpoint from the account name.
            var destStorageAccountString = string.Format("https://{0}.blob.core.windows.net", destStorageAccountName);


            //var destinationCredentials = new InteractiveBrowserCredential();


            //BlobServiceClient destBlobServiceClient = new BlobServiceClient(new Uri(destStorageAccountString), new DefaultAzureCredential());
            var aiaDestinationConnectionString = $@"DefaultEndpointsProtocol=https;AccountName=sath01seaudtp1;AccountKey=nb2WrmVX2AB4q/JeTR1xXuRbGUnkNqWn2ONd3+UeKQp8JCX0PGI7Sd2G/5vlkIvB/303xfJk8G/f+ASt8eBlYA==;BlobEndpoint=https://sath01seaudtp1.blob.core.windows.net/;QueueEndpoint=https://sath01seaudtp1.queue.core.windows.net/;TableEndpoint=https://sath01seaudtp1.table.core.windows.net/;FileEndpoint=https://sath01seaudtp1.file.core.windows.net/;";
           BlobServiceClient destBlobServiceClient = new BlobServiceClient(aiaDestinationConnectionString);


            BlobContainerClient destContainerClient = destBlobServiceClient.GetBlobContainerClient(destBlobContainerName);

            // Copy each blob from source to destination
          await DatalakeAPIHelper.CopyBlobAsync(client, sourceContainerUri.Replace("dfs", "blob"), blobPaths, destContainerClient,expiresOn);
        }
    }
}