﻿using Azure.Storage.Files.DataLake.Models;
using Azure.Storage.Files.DataLake;
using Azure;
using Newtonsoft.Json.Linq;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using Microsoft.Identity.Client;
using System.Net.Http.Headers;
using Azure.Storage.Blobs.Specialized;
using CopyStatus = Azure.Storage.Blobs.Models.CopyStatus;
using System.Threading.Tasks;
using System.Net.Http;
using System.Collections.Generic;
using System;
using System.Configuration;

namespace CustomReporting
{
    class DatalakeAPIHelper
    {
        // MSAL Public client app
        private static IPublicClientApplication application;

        public static async Task<string> GetSasTokenAsync(HttpClient httpClient, string requestUri)
        {
            string sasToken = string.Empty;
            try
            {
                // See https://docs.microsoft.com/powerapps/developer/data-platform/webapi/compose-http-requests-handle-errors
                // See https://docs.microsoft.com/powerapps/developer/data-platform/webapi/use-web-api-functions#unbound-functions
                var response = httpClient.GetAsync(requestUri).Result;

                if (response.IsSuccessStatusCode)
                {
                    // Parse the JSON formatted service response to obtain the user ID.
                    JObject body = JObject.Parse(
                        response.Content.ReadAsStringAsync().Result);
                    sasToken = (string)body["SASToken"];
                }
                else
                {
                    Console.WriteLine("The request failed with a status of '{0}'",
                                response.ReasonPhrase);
                }
            }
            catch (Exception ex)
            {
                HttpClientHelpers.DisplayException(ex);
                Console.WriteLine("Press any key to exit.");
                Console.ReadLine();
            }

            return sasToken;
        }


        public static List<string> ListBlobsWithSas(string containerPath, string sasToken)
        {
            Uri directoryUri = new Uri(containerPath);
            DataLakeDirectoryClient dataLakeDirectoryClient = new DataLakeDirectoryClient(directoryUri, new AzureSasCredential(sasToken));

            List<string> items = new List<string>();
            try
            {
                Pageable<PathItem> page = dataLakeDirectoryClient.GetPaths(recursive: true, true);
               
                foreach (PathItem path in page)
                {
                    Console.WriteLine(path.Name);
                    if (!(bool)path.IsDirectory)
                    {
                        items.Add(path.Name);
                    }
                    else
                    {
                        Console.WriteLine("Skip directory:" + path.Name);
                    }
                }
            }
            catch (RequestFailedException e)
            {
                // Check for a 403 (Forbidden) error. If the SAS is invalid, 
                // Azure Storage returns this error.
                if (e.Status == 403)
                {
                    Console.WriteLine("Blob listing operation failed for path {0}", containerPath);
                    Console.WriteLine("Additional error information: " + e.Message);
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine(e.Message);
                    Console.ReadLine();
                    throw;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return items;
        }

        public static async Task CopyBlobAsync(HttpClient client, string sourceContainerUri, List<string> blobs, BlobContainerClient destinationContainerClient, DateTime expiresOn)
        {
            try
            {
                foreach (string blob in blobs)
                {
                    ApplicationLog.Info($"Copying  from - {blob}");

                    // Get sas token for each blob to copy from source to destination
                    string sourceBlob = sourceContainerUri + "/" + blob;
                    string requestUri = string.Format("RetrieveAnalyticsStoreAccess(Url=@a,ResourceType='File',Permissions='Read,Write')?@a='{0}'", sourceBlob);

                    if (DateTime.Now.ToLocalTime().AddMinutes(15) > expiresOn.ToLocalTime())
                    {
                        string connectionString = ConfigurationManager.ConnectionStrings["Connect"].ConnectionString;

                        client = HttpClientHelpers.GetHttpClient(connectionString, HttpClientHelpers.clientId, out expiresOn, HttpClientHelpers.redirectUrl);
                    }
                    string token = GetSasTokenAsync(client, requestUri).Result;
                    string sourceSasUri = sourceBlob + token;
                    BlobClient sourceBlobClient = new BlobClient(new Uri(sourceSasUri));

                    // Get a BlobClient representing the destination blob with a unique name.
                    BlockBlobClient destBlob = destinationContainerClient.GetBlockBlobClient(blob);

                    ApplicationLog.Info($"Before moving to Destination - {destBlob.Uri}");

                    // Start the copy operation.
                    CopyFromUriOperation copyId = await destBlob.StartCopyFromUriAsync(sourceBlobClient.Uri);

                    await copyId.WaitForCompletionAsync();
                    ApplicationLog.Info($"Copy Done  to - {destBlob.Name}");

                    // Get the destination blob's properties and display the copy status.
                    BlobProperties destProperties = await destBlob.GetPropertiesAsync();
                    ApplicationLog.Info($"Copy status: {destProperties.CopyStatus}");
                    ApplicationLog.Info($"Copy progress: {destProperties.CopyProgress}");
                    ApplicationLog.Info($"Completion time: {destProperties.CopyCompletedOn}");


                    //Console.WriteLine($"Copy status: {destProperties.CopyStatus}");
                    //Console.WriteLine($"Copy progress: {destProperties.CopyProgress}");
                    //Console.WriteLine($"Completion time: {destProperties.CopyCompletedOn}");
                   // Console.WriteLine($"Total bytes: {destProperties.ContentLength}");

                  //  await WaitForPendingCopyToCompleteAsync(destBlob);
                }
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadLine();
                throw;
            }
        }

        private static async Task WaitForPendingCopyToCompleteAsync(BlockBlobClient toBlob)
        {
            var props = await toBlob.GetPropertiesAsync();
            while (props.Value.BlobCopyStatus == CopyStatus.Pending)
            {
                await Task.Delay(5000);
                props = await toBlob.GetPropertiesAsync();
            }

            if (props.Value.BlobCopyStatus != CopyStatus.Success)
            {
                throw new InvalidOperationException($"Copy failed: {props.Value.BlobCopyStatus}");
            }
        }
    }
}
