File Definitions

vsonlineRPaasConfig.txt 	- Defines the configuration for the Microsoft.VSOnline resource provider.
accounts_resourcetype.txt 	- [Deprecated]Defines the configuration for the accounts resource type, it is passed to rpaas when registerting the resource type. Accounts will be removed with the next api version.
plans_resourcetype.txt 		- Defines the configuration for the plans resource type. This file is sent to rpaas when registering the resource type.
operations_resourcetype.txt 	- Defines the configuration for the operations resource type. This is used for listing all operations available under the Resourece Provider.
sample_payload.txt 		- Defines a sample json payload for creating a plan resource type.
vsonlineAzureManifest.txt	- Defines the Microsoft.VSOnline namespace registered with Azure.
vsonline.json			- Swagger document defining Microsoft.VSOnline API.

Additional Information
armclient:
https://chocolatey.org/packages/ARMClient - Command line application for interaction with ARM API

Swagger:
***You must be a member of the Azure github team to view***
https://github.com/Azure/azure-rest-api-specs-pr/tree/RPSaaSMaster/specification/vsonline/resource-manager/Microsoft.VSOnline/

API Versions
2019-07-01-preview 		- Points to Production environment.
2019-07-01-beta    		- Points to PPE environment.
2019-07-01-alpha   		- Points to DEV environment.


HOW TO UPDATE RPAAS CONFIGURATION
The following scripts should be run in the order they are presented. You must JIT into the PROD subscription to make configuration changes.

Create Resource Provider 	- Registers the provided resource type configuration.
armclient.exe put subscriptions/979523fb-a19c-4bb0-a8ee-cef29597b0a4/providers/Microsoft.ProviderHub/providerregistrations/Microsoft.VSOnline?api-version=2019-02-01-preview @"C:\Users\you\vsonlineRPaasConfig.txt" -verbose

Get Resource Provider 		- Retrieves the configuration for the requested resource provider. 
armclient.exe get subscriptions/979523fb-a19c-4bb0-a8ee-cef29597b0a4/providers/Microsoft.ProviderHub/providerregistrations/Microsoft.VSOnline?api-version=2019-02-01-preview -verbose

Create Resource Type 		- Registers the provided resource type configuration.
armclient.exe put subscriptions/979523fb-a19c-4bb0-a8ee-cef29597b0a4/providers/Microsoft.ProviderHub/providerregistrations/Microsoft.VSOnline/resourcetyperegistrations/accounts?api-version=2019-02-01-preview @"C:\Users\you\accounts_resourcetype.txt" -verbose
armclient.exe put subscriptions/979523fb-a19c-4bb0-a8ee-cef29597b0a4/providers/Microsoft.ProviderHub/providerregistrations/Microsoft.VSOnline/resourcetyperegistrations/operations?api-version=2019-02-01-preview @"C:\Users\you\operations_resourcetype.txt" -verbose
armclient.exe put subscriptions/979523fb-a19c-4bb0-a8ee-cef29597b0a4/providers/Microsoft.ProviderHub/providerregistrations/Microsoft.VSOnline/resourcetyperegistrations/plans?api-version=2019-02-01-preview @"C:\Users\you\plan_resourcetype.txt" -verbose

Get Resource Type 		- Retrieves the configuration for the requested resource type.
armclient.exe get subscriptions/979523fb-a19c-4bb0-a8ee-cef29597b0a4/providers/Microsoft.ProviderHub/providerregistrations/Microsoft.VSOnline/resourcetyperegistrations/plans?api-version=2019-02-01-preview -verbose

Create Resource 		- Creates the specified resource type in Azure under the resource group. The resource group must exist or an error will be returned.
armclient.exe put subscriptions/979523fb-a19c-4bb0-a8ee-cef29597b0a4/resourcegroups/vs-online/providers/Microsoft.VSOnline/plans/PlanName1?api-version=2019-07-01-preview @"C:\Users\you\resources.txt" -verbose

Get Resource 			- Retrieves the resoruce type definition.
armclient.exe get subscriptions/979523fb-a19c-4bb0-a8ee-cef29597b0a4/resourcegroups/vs-online/providers/Microsoft.VSOnline/plans/PlanName1?api-version=2019-07-01-preview -verbose

Generate manifest 		- Generates the Microsoft.VSOnline manifest used to register the namespace with ARM.
armclient.exe post subscriptions/979523fb-a19c-4bb0-a8ee-cef29597b0a4/providers/Microsoft.ProviderHub/providerregistrations/Microsoft.VSOnline/generatemanifest?api-version=2019-02-01-preview  -verbose


HOW TO UPDATE AZURE MANIFEST
https://microsoft.sharepoint.com/teams/VSSaaS/_layouts/OneNote.aspx?id=%2Fteams%2FVSSaaS%2FShared%20Documents%2FGeneral%2FVS%20SaaS&wd=target%28DRI.one%7CAAA22431-7225-408C-8815-A43126E57042%2FHow%20to%20update%20VSOnline%20Resource%20Provider%7CDDDA88C7-8B86-4152-AEBD-BD9934D55088%2F%29
onenote:https://microsoft.sharepoint.com/teams/VSSaaS/Shared%20Documents/General/VS%20SaaS/DRI.one#How%20to%20update%20VSOnline%20Resource%20Provider&section-id={AAA22431-7225-408C-8815-A43126E57042}&page-id={DDDA88C7-8B86-4152-AEBD-BD9934D55088}&end
