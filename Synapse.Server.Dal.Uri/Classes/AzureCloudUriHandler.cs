using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Synapse.Server.Dal.Uri.Encryption;
using Synapse.Server.Dal.Uri.Interfaces;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synapse.Server.Dal.Uri
{
    public class AzureCloudUriHandler : ICloudUriHandler
    {
        public UriDalConfig UriDalConfig { get; set; }
        AzureStorageCredentials azureStorageCredentials { get; set; }
        public AzureCloudUriHandler(AzureUriDalConfig config)
        {
            this.UriDalConfig = config;
            azureStorageCredentials = config.CloudStorageCredentials;
        }

        public async Task<List<string>> ListFilesInFolder(string folderName)
        {
            CloudBlobContainer cloudBlobContainer = new CloudBlobContainer(new System.Uri(azureStorageCredentials.StorageUri),
                new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials(azureStorageCredentials.StorageToken));
            folderName = folderName.Replace("\\", "");
            CloudBlobDirectory blobDirectory = cloudBlobContainer.GetDirectoryReference(folderName);
            return await System.Threading.Tasks.Task.Run(() => blobDirectory.ListBlobs().Select(t => t.Uri.ToString()).ToList());
        }
        public async Task<string> GetFileInFolder(string folderName, string fileName)
        {
            CloudBlobContainer cloudBlobContainer = new CloudBlobContainer(new System.Uri(azureStorageCredentials.StorageUri),
                new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials(azureStorageCredentials.StorageToken));
            folderName = folderName.Replace("\\", "");
            CloudBlobDirectory blobDirectory = cloudBlobContainer.GetDirectoryReference(folderName);
            CloudBlob blob = blobDirectory.GetBlobReference(fileName);
            string text;
            using (var memoryStream = new MemoryStream())
            {
                blob.DownloadToStream(memoryStream);
                text = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
            }
            return await System.Threading.Tasks.Task.Run(() => EncryptionHelper.Decrypt(text));
        }
        public async Task WriteFileInFolder(string folderName, string fileName, string fileContent)
        {
            string ConnectionString = string.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}", azureStorageCredentials.StorageAccountName, azureStorageCredentials.StorageToken);
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConnectionString);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer blobContainer = blobClient.GetContainerReference(azureStorageCredentials.StorageContainerName);
            CloudBlobDirectory blobDirectory = blobContainer.GetDirectoryReference(folderName);
            CloudBlockBlob blockBlob = blobDirectory.GetBlockBlobReference(fileName);
            blockBlob.UploadText(fileContent);
            await Task.Delay(1);
        }



    }
}
