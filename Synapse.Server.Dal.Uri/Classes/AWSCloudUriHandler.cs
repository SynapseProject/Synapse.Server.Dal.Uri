using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Synapse.Server.Dal.Uri.Encryption;
using Synapse.Server.Dal.Uri.Interfaces;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;

namespace Synapse.Server.Dal.Uri
{
    public class AWSCloudUriHandler : ICloudUriHandler
    {
        public UriDalConfig UriDalConfig { get; set; }
        public string PlanPrefix { get; set; }
        private AmazonS3Client awsClient { get; set; }
        private AWSStorageCredentials awsStorageCredentials { get; set; }
        public AWSCloudUriHandler(AWSUriDalConfig config)
        {
            this.UriDalConfig = config;

            awsStorageCredentials
                = config.CloudStorageCredentials as AWSStorageCredentials;


            awsClient = new AmazonS3Client(awsStorageCredentials.AccessKey,
                awsStorageCredentials.Secret,
                Amazon.RegionEndpoint.GetBySystemName(awsStorageCredentials.AWSRegion));

            
        }

        public async Task<List<string>> ListFilesInFolder(string folderName)
        {
            List<string> plans = new List<string>();
            try
            {
                ListObjectsV2Request request = new ListObjectsV2Request
                {
                    MaxKeys = 1000,
                    BucketName = awsStorageCredentials.BucketName,
                    Prefix = PlanPrefix + "/" + folderName

                };
                ListObjectsV2Response response;
                do
                {
                    response = await awsClient.ListObjectsV2Async(request);
                    foreach (S3Object entry in response.S3Objects)
                    {
                        if (entry.Key.Length > request.Prefix.Length)
                            plans.Add(entry.Key);
                    }
                    request.ContinuationToken = response.NextContinuationToken;
                } while (response.IsTruncated);
            }
            catch (Exception ex)
            {
                int t = 0;
            }
            return plans;
        }
        public async Task<string> GetFileInFolder(string folderName, string fileName)
        {
            string text = string.Empty;
            try
            {
                GetObjectRequest request = new GetObjectRequest
                {
                    BucketName = awsStorageCredentials.BucketName,
                    Key = PlanPrefix + "/" + folderName + "/" + fileName

                };
                using (GetObjectResponse response = await awsClient.GetObjectAsync(request))
                using (Stream responseStream = response.ResponseStream)
                using (StreamReader reader = new StreamReader(responseStream))
                {
                    string title = response.Metadata["x-amz-meta-title"]; // Assume you have "title" as medata added to the object.
                    string contentType = response.Headers["Content-Type"];
                    Console.WriteLine("Object metadata, Title: {0}", title);
                    Console.WriteLine("Content type: {0}", contentType);
                    text = reader.ReadToEnd();
                }
            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine("Error encountered ***. Message:'{0}' when writing an object", e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unknown encountered on server. Message:'{0}' when writing an object", e.Message);
            }
            return EncryptionHelper.Decrypt(text);
        }
        public async Task WriteFileInFolder(string folderName, string fileName, string fileContent)
        {
            var fileTransferUtility =
                    new TransferUtility(awsClient);
            string keyName = PlanPrefix + "/" + UriDalConfig.HistoryFolderPath + "/" + fileName;
            await fileTransferUtility.UploadAsync(GenerateStreamFromString(EncryptionHelper.Encrypt(fileContent)), awsStorageCredentials.BucketName, keyName);
        }

        private Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public AzureStorageCredentials Credentials { get; set; } = new AzureStorageCredentials();
    }
}
