using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DriveSupplyCollectorBase;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Blob;
using S2.BlackSwan.SupplyCollector.Models;

namespace AzureBlobSupplyCollector
{
    public class AzureBlobSupplyCollector : DriveSupplyCollectorBase.DriveSupplyCollectorBase {
        private CloudStorageAccount _storageAccount;
        private CloudBlobClient _blobClient;
        private string _container;

        private const string PREFIX = "azureblob://";

        public AzureBlobSupplyCollector(string s2Prefix = null, int s2FolderLevels = 0, bool s2UseFileNameInDcName = false) : base(s2Prefix, s2FolderLevels, s2UseFileNameInDcName) {
        }

        public override List<string> DataStoreTypes()
        {
            return (new[] { "AzureBlob" }).ToList();
        }

        public string BuildConnectionString(string accountName, string accountKey, string container) {
            var attrs = new StringBuilder();
            if (s2Prefix != null) {
                attrs.Append($",s2-prefix={s2Prefix};");
            }
            if (s2FolderLevels != 0) {
                attrs.Append($",s2-folder-levels-used-in-dc-name={s2FolderLevels};");
            }
            if (s2UseFileNameInDcName) {
                attrs.Append(",s2-use-file-name-in-dc-name=True;");
            }
            return $"{PREFIX}{accountName}:{accountKey}/{container}{attrs}";
        }

        private void Connect(string connectString) {
            if (_blobClient != null) {
                return;
            }

            if (!connectString.StartsWith(PREFIX))
                throw new ArgumentException("Invalid connection string!");

            var accountIndex = PREFIX.Length;
            var keyIndex = connectString.IndexOf(":", accountIndex);
            if (keyIndex <= 0)
                throw new ArgumentException("Invalid connection string!");
            var containerIndex = connectString.LastIndexOf("/");
            if (containerIndex <= 0)
                throw new ArgumentException("Invalid connection string!");
            var additionsIndex = connectString.IndexOf(",", containerIndex);

            var account = connectString.Substring(accountIndex, keyIndex - accountIndex);
            var accountKey = connectString.Substring(keyIndex + 1, containerIndex - keyIndex - 1);
            
            if (additionsIndex > 0)
            {
                _container = connectString.Substring(containerIndex + 1, additionsIndex - containerIndex - 1);
                ParseConnectionStringAdditions(connectString.Substring(additionsIndex + 1));
            }
            else
            {
                _container = connectString.Substring(containerIndex + 1);
            }

            _storageAccount = new CloudStorageAccount(
                new StorageCredentials(
                    account,
                    accountKey), true);
            _blobClient = _storageAccount.CreateCloudBlobClient();
        }

        public override bool TestConnection(DataContainer container)
        {
            try {
                Connect(container.ConnectionString);

                return true;
            }
            catch (Exception) {
                return false;
            }
        }

        protected override Stream GetFileStream(DataContainer container, string filePath) {
            Connect(container.ConnectionString);

            var containerRef = _blobClient.GetContainerReference(_container);

            var blob = containerRef.GetBlockBlobReference(filePath);
            return blob.OpenRead();
        }

        protected override List<DriveFileInfo> ListDriveFiles(DataContainer container) {
            Connect(container.ConnectionString);

            var files = new List<DriveFileInfo>();

            var containerRef = _blobClient.GetContainerReference(_container);
            var blobs = containerRef.ListBlobs(s2Prefix, true);
            foreach (var item in blobs) {
                if (item.GetType() == typeof(CloudBlockBlob))
                {
                    var blob = (CloudBlockBlob)item;
                    
                    files.Add(new DriveFileInfo() {
                        FilePath = blob.Name,
                        FileSize = blob.Properties.Length
                    });
                }
            }

            return files;
        }
    }
}
