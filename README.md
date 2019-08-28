# AzureBlobSupplyCollector
A supply collector designed to connect to AzureBlob and parse all supported file types

## Build
Run `dotnet build`

## Tests
Run `./run-tests.sh`

## Adding test data

* Run docker container which has access to test data storage
`docker run -d -p 10000:10000 --name azurite1 --mount source=$(pwd)/AzureBlobSupplyCollectorTests/tests,target=/data,type=bind mcr.microsoft.com/azure-storage/azurite`
* Download Azure Storage Explorer and connect using following connection string:
`DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;`
* Change ip address if necessary
* Upload/change/remove files
* Stop and remove container:
```
docker stop azurite1
docker rm azurite1
```
* Commit changes to test data storage
