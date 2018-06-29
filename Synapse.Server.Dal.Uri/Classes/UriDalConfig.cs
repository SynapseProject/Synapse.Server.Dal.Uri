using Synapse.Server.Dal.Enums;
using System.Configuration;

namespace Synapse.Server.Dal.Uri
{
    public class UriDalConfig
    {
        public string PlanFolderPath { get; set; } = "Plans";
        public string HistoryFolderPath { get; set; } = "History";
        public bool ProcessPlansOnSingleton { get; set; } = false;
        public bool ProcessActionsOnSingleton { get; set; } = true;
        public CloudPlatform CloudPlatform { get; set; }
        public SecurityConfig Security { get; set; } = new SecurityConfig();
        public string PublicKey { get; set; }
        public string PrivateKey { get; set; }
        public string Vector { get; set; }
        public bool EncryptionEnabled { get; set; }
    }
    public class AWSUriDalConfig: UriDalConfig
    {
        public AWSStorageCredentials CloudStorageCredentials { get; set; }
    }
    public class AzureUriDalConfig : UriDalConfig
    {
        public AzureStorageCredentials CloudStorageCredentials { get; set; }
    }

    public class SecurityConfig
    {
        public string FilePath { get; set; } = "Security";
        public bool IsRequired { get; set; } = false;
        public bool ValidateSignature { get; set; } = false;
        public string SignaturePublicKeyFile { get; set; }
        public string GlobalExternalGroupsCsv { get; set; } = "Everyone";
    }
    public class CloudStorageCredentials
    {

    }
    public class AWSStorageCredentials : CloudStorageCredentials
    {
        public string AWSRegion { get; set; }
        public string BucketName { get; set; }
        public string AccessKey { get; set; }
        public string Secret { get; set; }
        public string PlanPrefix { get; set; }  
    }
    public class AzureStorageCredentials : CloudStorageCredentials
    {
        public string StorageAccountName { get; set; }
        public string StorageToken { get; set; }
        public string StorageUri { get; set; }
        public string StorageContainerName { get; set; }
    }
}
