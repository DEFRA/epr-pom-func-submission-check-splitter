# EPR Submission Check Splitter

## Overview

This function listens to an Azure ServiceBus Queue, for messages indicating a user has uploaded POM a file via the front end, to blob storage. 
It retrieves the file, reads the rows, groups the rows by producer ID, and adds a message to another Azure ServiceBus Queue per producer, 
to be consumed by the Validation Func.
 
## How To Run
 
### Prerequisites 
In order to run the service you will need the following dependencies 
 
- .NET 6
- Azure CLI
- Azurite
 
### Run 
Go to `src/SubmissionCheckSplitter.Functions` directory and execute:

```
func start
```

### Docker
Run in terminal at the solution source root:

```
docker build -t submissionchecksplitter -f SubmissionCheckSplitter.Functions/Dockerfile .
```

Fill out the environment variables and run the following command -
```
docker run -e AzureWebJobsStorage="X" -e FUNCTIONS_EXTENSION_VERSION="X" -e FUNCTIONS_WORKER_RUNTIME="X" -e ServiceBus:ConnectionString="X" -e ServiceBus:SplitQueueName="X" -e ServiceBus:UploadQueueName="X" -e StorageAccount:ConnectionString="X" -e StorageAccount:PomBlobContainerName="X" -e SubmissionApi:BaseUrl="X" submissionchecksplitter
```

## How To Test

To run the function app locally, Azurite must be set up and running in order to fetch a file from blob storage. Add a POM file in the storage emulator and
include the blob name as part of the payload.

A service bus must also be configured in order to send the payload for the function to pick up. This can be set up locally via Azurite or configured to an Azure service bus instance.

### Payload

An example of the payload is given here:

```json
{
  "blobName": "",
  "submissionId": "",
  "userId": "",
  "organisationId": "",
  "submissionPeriod": ""
}
```

NOTE: Only the blob name is a required field, the others are optional.
 
### Unit tests 

On root directory `src`, execute:

```
dotnet test
```
 
### Pact tests 
 
N/A
 
### Integration tests

N/A
 
## How To Debug 

Use debugging tools in your chosen IDE. 
 
## Environment Variables - deployed environments 
The structure of the appsettings can be found in the repository. Example configurations for the different environments can be found in [epr-app-config-settings](https://dev.azure.com/defragovuk/RWD-CPR-EPR4P-ADO/_git/epr-app-config-settings).

| Variable Name                        | Description                                                                                             |
| ------------------------------------ | ------------------------------------------------------------------------------------------------------- |
| AzureWebJobsStorage                  | The connection string for the Azure Web Jobs Storage                                                    |
| FUNCTIONS_EXTENSION_VERSION          | The extension version for Azure Function - i.e. ~4                                                      |
| FUNCTIONS_WORKER_RUNTIME             | The runtime name for the Azure Function - i.e. `dotnet`                                                 |
| ServiceBus__ConnectionString         | The connection string for the service bus                                                               |
| ServiceBus__SplitQueueName           | The name of the split queue                                                                             |
| ServiceBus__UploadQueueName          | The name of the upload queue                                                                            |
| StorageAccount__ConnectionString     | The connection string of the blob container on the storage account, where uploaded files will be stored |
| StorageAccount__PomBlobContainerName | The name of the blob container on the storage account, where uploaded POM files will be stored          |
| SubmissionApi__BaseUrl               | The base URL for the Submission Status API WebApp                                                       |

## Additional Information 

See [ADR-012.A: EPR Phase 1 - Compliance Scheme PoM Data Upload](https://eaflood.atlassian.net/wiki/spaces/MWR/pages/4251418625/ADR-012.A+EPR+Phase+1+-+Compliance+Scheme+PoM+Data+Upload)

### Monitoring and Health Check 

Enable Health Check in the Azure portal and set the URL path to ```ServiceBusQueueTrigger```

## Directory Structure 

### Source files 
- `SubmissionCheckSplitter.Application` - Application .NET source files
- `SubmissionCheckSplitter.Data` - Data .NET source files
- `SubmissionCheckSplitter.Functions` - Function .NET source files
- `SubmissionCheckSplitter.UnitTests` - .NET unit test files

## Contributing to this project

Please read the [contribution guidelines](CONTRIBUTING.md) before submitting a pull request.

## Licence

[Licence information](LICENCE.md).
