#!/bin/bash
docker run -d -p 10000:10000 --name azurite1 --mount source=$(pwd)/AzureBlobSupplyCollectorTests/tests,target=/data,type=bind mcr.microsoft.com/azure-storage/azurite

echo { > AzureBlobSupplyCollectorTests/Properties/launchSettings.json
echo   \"profiles\": { >> AzureBlobSupplyCollectorTests/Properties/launchSettings.json
echo     \"AzureBlobSupplyCollectorTests\": { >> AzureBlobSupplyCollectorTests/Properties/launchSettings.json
echo       \"commandName\": \"Project\", >> AzureBlobSupplyCollectorTests/Properties/launchSettings.json
echo       \"environmentVariables\": { >> AzureBlobSupplyCollectorTests/Properties/launchSettings.json
echo         \"AZUREBLOB_CONTAINER\": \"testblob\", >> AzureBlobSupplyCollectorTests/Properties/launchSettings.json
echo         \"AZUREBLOB_ACCOUNT_NAME\": \"devstoreaccount1\", >> AzureBlobSupplyCollectorTests/Properties/launchSettings.json
echo         \"AZUREBLOB_ACCOUNT_KEY\": \"Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==\", >> AzureBlobSupplyCollectorTests/Properties/launchSettings.json
echo         \"AZUREBLOB_HOST\": \"http://127.0.0.1:10000/devstoreaccount1\" >> AzureBlobSupplyCollectorTests/Properties/launchSettings.json
echo       } >> AzureBlobSupplyCollectorTests/Properties/launchSettings.json
echo     } >> AzureBlobSupplyCollectorTests/Properties/launchSettings.json
echo   } >> AzureBlobSupplyCollectorTests/Properties/launchSettings.json
echo } >> AzureBlobSupplyCollectorTests/Properties/launchSettings.json

dotnet build
dotnet test
docker stop azurite1
docker rm azurite1
