using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Blob;
using Parquet.Thrift;
using S2.BlackSwan.SupplyCollector.Models;
using SupplyCollectorDataLoader;

namespace AzureBlobSupplyCollectorLoader
{
    public class AzureBlobSupplyCollectorLoader : SupplyCollectorDataLoaderBase
    {
        private CloudStorageAccount _storageAccount;
        private CloudBlobClient _blobClient;
        private string _container;
        private string _overrideHost;
        protected string _s2Prefix;
        protected int _s2FolderLevels = 0;
        protected bool _s2UseFileNameInDcName = false;
        protected bool _csvHasHeader = true;

        private const string PREFIX = "azureblob://";

        private void ParseConnectionStringAdditions(string additions)
        {
            var parts = additions.Split(",");
            foreach (var part in parts)
            {
                if (String.IsNullOrEmpty(part))
                    continue;

                var pair = part.Split("=");
                if (pair.Length == 2)
                {
                    if ("s2-prefix".Equals(pair[0]))
                    {
                        _s2Prefix = pair[1];
                    }
                    else if ("s2-folder-levels-used-in-dc-name".Equals(pair[0]))
                    {
                        _s2FolderLevels = Int32.Parse(pair[1]);
                    }
                    else if ("s2-use-file-name-in-dc-name".Equals(pair[0]))
                    {
                        _s2UseFileNameInDcName = Boolean.Parse(pair[1]);
                    }
                    else if ("csv_has_header".Equals(pair[0]))
                    {
                        _csvHasHeader = Boolean.Parse(pair[1]);
                    } else if ("override_host".Equals(pair[0]))
                    {
                        _overrideHost = pair[1];
                    }
                }
            }
        }

        private void Connect(string connectString)
        {
            if (_blobClient != null)
            {
                return;
            }

            if (!connectString.StartsWith(PREFIX))
                throw new ArgumentException("Invalid connection string!");

            var accountIndex = PREFIX.Length;
            var keyIndex = connectString.IndexOf(":", accountIndex);
            if (keyIndex <= 0)
                throw new ArgumentException("Invalid connection string!");
            var additionsIndex = connectString.IndexOf(",", keyIndex);
            var containerIndex = additionsIndex > 0 ? connectString.LastIndexOf("/", additionsIndex) : connectString.LastIndexOf("/");
            if (containerIndex <= 0)
                throw new ArgumentException("Invalid connection string!");

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

            if (_overrideHost != null)
            {
                _storageAccount = new CloudStorageAccount(
                    new StorageCredentials(
                        account,
                        accountKey), new Uri(_overrideHost), null, null, null);
            }
            else
            {
                _storageAccount = new CloudStorageAccount(
                    new StorageCredentials(
                        account,
                        accountKey), true);
            }

            _blobClient = _storageAccount.CreateCloudBlobClient();
        }

        public override void InitializeDatabase(DataContainer dataContainer) {
            Connect(dataContainer.ConnectionString);

            var container = _blobClient.GetContainerReference(_container);
            container.CreateIfNotExists(BlobContainerPublicAccessType.Blob);
        }

        public override void LoadSamples(DataEntity[] dataEntities, long count) {
            Console.WriteLine("... connecting");
            Connect(dataEntities[0].Container.ConnectionString);
            Console.Write("... generating samples: ");
            var path = Path.GetTempFileName();
            using (var writer = new StreamWriter(path, false, System.Text.Encoding.UTF8)) {
                if (_csvHasHeader) {
                    writer.WriteLine(String.Join(", ", dataEntities.Select(x => x.Name).ToArray()));
                }

                var r = new Random();
                long rows = 0;
                while (rows < count) {
                    if(rows % 1000 == 0) 
                        Console.Write(".");

                    bool first = true;
                    foreach (var dataEntity in dataEntities) {
                        if (!first)
                            writer.Write(", ");

                        switch (dataEntity.DataType) {
                            case DataType.String:
                                writer.Write(Guid.NewGuid().ToString());
                                break;
                            case DataType.Int:
                                writer.Write(r.Next().ToString());
                                break;
                            case DataType.Double:
                                writer.Write(r.NextDouble().ToString().Replace(",", "."));
                                break;
                            case DataType.Boolean:
                                writer.Write(r.Next(100) > 50 ? "true" : "false");
                                break;
                            case DataType.DateTime:
                                var val = DateTimeOffset
                                    .FromUnixTimeMilliseconds(
                                        DateTimeOffset.Now.ToUnixTimeMilliseconds() + r.Next()).DateTime;

                                writer.Write(val.ToString("s"));
                                break;
                            default:
                                writer.Write(r.Next().ToString());
                                break;
                        }

                        first = false;
                    }

                    writer.WriteLine();
                    rows++;
                }
            }
            Console.WriteLine();

            Console.WriteLine("... uploading to Azure");
            string remotePath;
            if (_s2UseFileNameInDcName) {
                remotePath = _s2Prefix ?? "" + "/" + dataEntities[0].Collection.Name + "/" + dataEntities[0].Collection.Name + ".csv";
            }
            else {
                remotePath = _s2Prefix ?? "" + "/" + dataEntities[0].Collection.Name + "/" + Guid.NewGuid() + ".csv";
            }

            var containerRef = _blobClient.GetContainerReference(_container);
            var blobRef = containerRef.GetBlockBlobReference(remotePath);

            blobRef.UploadFromFile(path);

            File.Delete(path);
        }

        private string[] ListTestFiles(string path, string root = null) {
            Console.WriteLine($"ListTestFiles({path}, {root})");

            var list = new List<string>();

            var dirs = Directory.GetDirectories(path);
            foreach (var dir in dirs) {
                if(dir.Equals(".") || dir.Equals(".."))
                    continue;

                Console.WriteLine($"... found dir={dir}");
                var dirName = Path.GetFileName(dir);

                list.AddRange(ListTestFiles($"{path}/{dirName}", (root == null ? "" : (root + "/")) + dirName));
            }

            var files = Directory.GetFiles(path);
            foreach (var file in files) {
                var fileName = Path.GetFileName(file);
                list.Add($"{root}/{fileName}");
            }

            return list.ToArray();
        }

        public override void LoadUnitTestData(DataContainer dataContainer) {
            Connect(dataContainer.ConnectionString);

            var containerRef = _blobClient.GetContainerReference(_container);

            var files = ListTestFiles("tests");
            foreach (var file in files) {
                var blobRef = containerRef.GetBlockBlobReference(file);

                blobRef.UploadFromFile($"tests/{file}");
            }
        }
    }
}
