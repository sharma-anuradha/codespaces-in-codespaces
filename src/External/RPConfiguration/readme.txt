Folders
ARM		- Contains files that are checked into ARM repositories. These files define the contract between ARM and our RP service.
		  https://github.com/Azure/azure-rest-api-specs-pr/tree/RPSaaSMaster/specification/vsonline
		  https://armmanifest.visualstudio.com/One/_git/Manifest?path=%2FMICROSOFT.VSONLINE&version=GBmaster
		Files:
			vsonlineAzureManifest.txt	- Defines the Microsoft.VSOnline namespace including resource types registered with ARM. 
			vsonline					- Contains all api version files of the Microsoft.VSOnline swagger document.

Portal	- Contains our Azure Portal definition files. These files define the look and behavior of our RP portal experience.
		Files:
			PortalDefinition.json		- Defines verbage used to reference our resource type in the portal, api version, and icon image
			viewDefinition.json			- Defines the text and images of our portal Overview page

RPaaS	- Contains configuration files used to register our Resource provider and resource types with the RPaaS service.
		Files:
			vsonlineRPaasConfig.txt 	- Defines the configuration for the Microsoft.VSOnline resource provider.
			accounts_resourcetype.txt 	- [Deprecated]Defines the configuration for the accounts resource type, it is passed to rpaas when registerting the resource type. Accounts will be removed with the next api version.
			plans_resourcetype.txt 		- Defines the configuration for the plans resource type. This file is sent to rpaas when registering the resource type.
			operations_resourcetype.txt - Defines the configuration for the operations resource type. This is used for listing all operations available under the Resourece Provider.
			sample_payload.txt 			- Defines a sample json payload for creating a plan resource type.

API Versions
2019-07-01-preview 		- Points to Production resource provider.
2019-07-01-beta    		- Points to PPE resource provider.
2019-07-01-alpha   		- Points to DEV resource provider.
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

HOW TO UPDATE RPAAS CONFIGURATION
https://microsoft.sharepoint.com/teams/VSSaaS/_layouts/OneNote.aspx?id=%2Fteams%2FVSSaaS%2FShared%20Documents%2FGeneral%2FVS%20SaaS&wd=target%28DRI.one%7CAAA22431-7225-408C-8815-A43126E57042%2FHow%20to%20update%20RPaaS%20Configuration%7CA2136D9D-43E1-4A6E-956F-1D7D5129D8ED%2F%29
onenote:https://microsoft.sharepoint.com/teams/VSSaaS/Shared%20Documents/General/VS%20SaaS/DRI.one#How%20to%20update%20RPaaS%20Configuration&section-id={AAA22431-7225-408C-8815-A43126E57042}&page-id={A2136D9D-43E1-4A6E-956F-1D7D5129D8ED}&end

HOW TO UPDATE AZURE MANIFEST AND SWAGGER Doc
https://microsoft.sharepoint.com/teams/VSSaaS/_layouts/OneNote.aspx?id=%2Fteams%2FVSSaaS%2FShared%20Documents%2FGeneral%2FVS%20SaaS&wd=target%28DRI.one%7CAAA22431-7225-408C-8815-A43126E57042%2FHow%20to%20update%20VSOnline%20Resource%20Provider%7CDDDA88C7-8B86-4152-AEBD-BD9934D55088%2F%29
onenote:https://microsoft.sharepoint.com/teams/VSSaaS/Shared%20Documents/General/VS%20SaaS/DRI.one#How%20to%20update%20VSOnline%20Resource%20Provider&section-id={AAA22431-7225-408C-8815-A43126E57042}&page-id={DDDA88C7-8B86-4152-AEBD-BD9934D55088}&end
