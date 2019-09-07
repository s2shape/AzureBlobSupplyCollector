#!/bin/bash
docker run -d -p 10000:10000 --name azurite1 --mount source=$(pwd)/AzureBlobSupplyCollectorTests/tests,target=/data,type=bind mcr.microsoft.com/azure-storage/azurite

mkdir AzureBlobSupplyCollectorTests/Properties

export AZUREBLOB_CONTAINER=testblob
export AZUREBLOB_ACCOUNT_NAME=devstoreaccount1
export AZUREBLOB_ACCOUNT_KEY="Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw=="
export AZUREBLOB_HOST="http://127.0.0.1:10000/devstoreaccount1"

dotnet build
dotnet test
docker stop azurite1
docker rm azurite1
