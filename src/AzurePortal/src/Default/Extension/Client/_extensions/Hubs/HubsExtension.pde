{
  "extension": "HubsExtension",
  "version": "6.317.0.5162913322.200629-2313",
  "sdkVersion": "6.317.0.5 (production#1629c13322.200629-2313) Signed",
  "assetTypes": [
    {
      "name": "ArmExplorer",
      "permissions": [],
      "links": null
    },
    {
      "name": "BrowseAll",
      "permissions": [],
      "links": null
    },
    {
      "name": "NonAssetResource",
      "permissions": [],
      "links": null
    },
    {
      "name": "BrowseInstanceLink",
      "permissions": [],
      "links": null
    },
    {
      "name": "BrowseResourceGroup",
      "permissions": [],
      "links": null
    },
    {
      "name": "BrowseResource",
      "permissions": [],
      "links": null
    },
    {
      "name": "BrowseAllResources",
      "permissions": [],
      "links": null
    },
    {
      "name": "BrowseRecentResources",
      "permissions": [],
      "links": null
    },
    {
      "name": "ResourceGraphExplorer",
      "permissions": [],
      "links": null
    },
    {
      "name": "Dashboards",
      "permissions": [],
      "links": null
    },
    {
      "name": "WhatsNew",
      "permissions": [],
      "links": null
    },
    {
      "name": "Deployments",
      "permissions": [],
      "links": null
    },
    {
      "name": "ResourceGroups",
      "permissions": [
        {
          "Name": "read",
          "Action": "Microsoft.Resources/subscriptions/resourceGroups/read"
        },
        {
          "Name": "deleteObject",
          "Action": "Microsoft.Resources/subscriptions/resourceGroups/delete"
        },
        {
          "Name": "write",
          "Action": "Microsoft.Resources/subscriptions/resourceGroups/write"
        },
        {
          "Name": "writeDeployments",
          "Action": "Microsoft.Resources/subscriptions/resourceGroups/deployments/write"
        },
        {
          "Name": "readDeployments",
          "Action": "Microsoft.Resources/subscriptions/resourceGroups/deployments/read"
        },
        {
          "Name": "readEvents",
          "Action": "Microsoft.Insights/events/read"
        }
      ],
      "links": null
    },
    {
      "name": "Tag",
      "permissions": [],
      "links": null
    },
    {
      "name": "ARGSharedQueries",
      "permissions": [],
      "links": null
    },
    {
      "name": "ARGStorageAccounts",
      "permissions": [],
      "links": null
    }
  ],
  "parts": [
    {
      "name": "MonitorChartPart",
      "inputs": [],
      "commandBindings": [],
      "initialSize": 5,
      "typeScriptMetadata": {
        "ParametersTypeExpression": {
          "TypeExpression": "HubsExtension.MonitorChartPart.Parameters"
        },
        "FilterModelsTypeExpression": {
          "TypeExpression": "{ [filterId: string]: FxFilters.AnyFilterModel; }"
        }
      },
      "supportedSizes": [
        3,
        4,
        10,
        5,
        6
      ]
    },
    {
      "name": "ResourceTagsTile",
      "inputs": [],
      "commandBindings": [],
      "initialSize": 10,
      "typeScriptMetadata": {
        "ParametersTypeExpression": {
          "TypeExpression": "HubsExtension.ResourceTagsBlade.Parameters"
        },
        "FilterModelsTypeExpression": {
          "TypeExpression": "{ [filterId: string]: FxFilters.AnyFilterModel; }"
        }
      },
      "supportedSizes": [
        1,
        2,
        3,
        10,
        5,
        6,
        11
      ]
    },
    {
      "name": "TagsTile",
      "inputs": [],
      "commandBindings": [],
      "initialSize": 10,
      "typeScriptMetadata": {
        "ParametersTypeExpression": {
          "TypeExpression": "HubsExtension.TagsBlade.Parameters"
        },
        "FilterModelsTypeExpression": {
          "TypeExpression": "{ [filterId: string]: FxFilters.AnyFilterModel; }"
        }
      },
      "supportedSizes": [
        1,
        2,
        3,
        10,
        5,
        6,
        11
      ]
    },
    {
      "name": "BrowseServicePart",
      "inputs": [
        "assetTypeId"
      ],
      "commandBindings": [],
      "initialSize": 2
    },
    {
      "name": "ResourceGroupMapPart",
      "inputs": [
        "resourceGroup"
      ],
      "commandBindings": [],
      "initialSize": 5,
      "supportedSizes": [
        1,
        2,
        3,
        10,
        5,
        6,
        11
      ]
    },
    {
      "name": "ResourceMapPart",
      "inputs": [
        "assetOwner",
        "assetType",
        "assetId"
      ],
      "commandBindings": [],
      "initialSize": 5,
      "supportedSizes": [
        1,
        2,
        3,
        10,
        5,
        6,
        11
      ]
    },
    {
      "name": "Resources",
      "inputs": [],
      "commandBindings": [],
      "initialSize": 8,
      "supportedSizes": []
    },
    {
      "name": "GettingStartedPart",
      "inputs": [],
      "commandBindings": [],
      "initialSize": 99,
      "initialHeight": 5,
      "initialWidth": 4,
      "supportedSizes": []
    },
    {
      "name": "DiagnosticsTile",
      "inputs": [],
      "commandBindings": [],
      "initialSize": 1
    },
    {
      "name": "WhatsNewV1Tile",
      "inputs": [],
      "commandBindings": [],
      "initialSize": 2
    },
    {
      "name": "FeedbackTile",
      "inputs": [],
      "commandBindings": [],
      "initialSize": 0
    },
    {
      "name": "ResourcePart",
      "inputs": [],
      "commandBindings": [],
      "initialSize": 2,
      "typeScriptMetadata": {
        "ParametersTypeExpression": {
          "TypeExpression": "{ id: string }"
        },
        "FilterModelsTypeExpression": null
      }
    },
    {
      "name": "SpecPickerListViewPart",
      "inputs": [],
      "commandBindings": [],
      "initialSize": 9,
      "supportedSizes": []
    },
    {
      "name": "PricingTierLauncher",
      "inputs": [
        "entityId"
      ],
      "commandBindings": [],
      "initialSize": 3,
      "supportedSizes": [
        0,
        1,
        2,
        3
      ]
    },
    {
      "name": "SpecComparisonPart",
      "inputs": [],
      "commandBindings": [],
      "initialSize": 8,
      "supportedSizes": []
    },
    {
      "name": "SpecPickerListViewPartV3",
      "inputs": [],
      "commandBindings": [],
      "initialSize": 9,
      "parameterProvider": true,
      "supportedSizes": []
    },
    {
      "name": "SpecPickerGridViewPartV3",
      "inputs": [],
      "commandBindings": [],
      "initialSize": 9,
      "parameterProvider": true,
      "supportedSizes": []
    },
    {
      "name": "PricingTierLauncherV3",
      "inputs": [
        "entityId"
      ],
      "commandBindings": [],
      "initialSize": 3,
      "supportedSizes": [
        0,
        1,
        2,
        3
      ]
    },
    {
      "name": "ResourceFilterPart",
      "inputs": [],
      "commandBindings": [],
      "initialSize": 8,
      "supportedSizes": []
    },
    {
      "name": "ResourceTagsPart",
      "inputs": [
        "resourceId"
      ],
      "commandBindings": [],
      "initialSize": null,
      "supportedSizes": []
    }
  ],
  "partTypeScriptDependencies": {
    "DefinitionFileNames": [
      {
        "FileName": "HubsExtension.d.ts"
      }
    ],
    "Modules": null
  },
  "blades": [
    {
      "name": "ARGBrowseAllInMenu",
      "keyParameters": [],
      "inputs": [],
      "optionalInputs": [
        "filter",
        "scope"
      ],
      "outputs": [],
      "typeScriptMetadata": {
        "ParametersTypeExpression": {
          "TypeExpression": "{ filter?: string; scope?: string; }"
        },
        "ReturnedDataTypeExpression": null
      }
    },
    {
      "name": "ARGBrowseResourceGroupsInMenu",
      "keyParameters": [],
      "inputs": [],
      "optionalInputs": [
        "filter",
        "scope"
      ],
      "outputs": [],
      "typeScriptMetadata": {
        "ParametersTypeExpression": {
          "TypeExpression": "{ filter?: string; scope?: string; }"
        },
        "ReturnedDataTypeExpression": null
      }
    },
    {
      "name": "ArgQueryBlade",
      "keyParameters": [],
      "inputs": [],
      "optionalInputs": [
        "name",
        "query",
        "queryId",
        "description",
        "isShared",
        "formatResults",
        "fromPinnedPart"
      ],
      "outputs": [],
      "typeScriptMetadata": {
        "ParametersTypeExpression": {
          "TypeExpression": "HubsExtension.ArgQueryBlade.Parameters"
        },
        "ReturnedDataTypeExpression": {
          "TypeExpression": "HubsExtension.ArgQueryBlade.QueryBladeResult"
        }
      }
    },
    {
      "name": "BrowseAll",
      "keyParameters": [],
      "inputs": [],
      "optionalInputs": [
        "filter",
        "tagName",
        "tagValue"
      ],
      "outputs": [],
      "typeScriptMetadata": {
        "ParametersTypeExpression": {
          "TypeExpression": "HubsExtension.BrowseAll.Parameters"
        },
        "ReturnedDataTypeExpression": null
      }
    },
    {
      "name": "BrowseQuery",
      "keyParameters": [],
      "inputs": [
        "query",
        "title"
      ],
      "optionalInputs": [],
      "outputs": [],
      "typeScriptMetadata": {
        "ParametersTypeExpression": {
          "TypeExpression": "HubsExtension.BrowseQuery.Parameters"
        },
        "ReturnedDataTypeExpression": null
      }
    },
    {
      "name": "BrowseResource",
      "keyParameters": [],
      "inputs": [
        "resourceType"
      ],
      "optionalInputs": [
        "filter",
        "kind"
      ],
      "outputs": [],
      "typeScriptMetadata": {
        "ParametersTypeExpression": {
          "TypeExpression": "HubsExtension.BrowseResource.Parameters"
        },
        "ReturnedDataTypeExpression": null
      }
    },
    {
      "name": "BrowseResourceGroups",
      "keyParameters": [],
      "inputs": [],
      "optionalInputs": [
        "filter"
      ],
      "outputs": [],
      "typeScriptMetadata": {
        "ParametersTypeExpression": {
          "TypeExpression": "{ filter?: string; }"
        },
        "ReturnedDataTypeExpression": null
      }
    },
    {
      "name": "BrowseResourcesWithTag",
      "keyParameters": [],
      "inputs": [
        "tagName",
        "tagValue"
      ],
      "optionalInputs": [
        "filter"
      ],
      "outputs": [],
      "typeScriptMetadata": {
        "ParametersTypeExpression": {
          "TypeExpression": "HubsExtension.BrowseResourcesWithTag.Parameters"
        },
        "ReturnedDataTypeExpression": null
      }
    },
    {
      "name": "ResourceGroupOverview",
      "keyParameters": [],
      "inputs": [
        "id"
      ],
      "optionalInputs": [],
      "outputs": [],
      "typeScriptMetadata": {
        "ParametersTypeExpression": {
          "TypeExpression": "{ id: string; }"
        },
        "ReturnedDataTypeExpression": null
      }
    },
    {
      "name": "ResourceProperties",
      "keyParameters": [],
      "inputs": [
        "id"
      ],
      "optionalInputs": [
        "overrideIcon"
      ],
      "outputs": [],
      "typeScriptMetadata": {
        "ParametersTypeExpression": {
          "TypeExpression": "HubsExtension.ResourcePropertiesBlade.Parameters"
        },
        "ReturnedDataTypeExpression": null
      }
    },
    {
      "name": "TemplateEditorBladeV2",
      "keyParameters": [],
      "inputs": [],
      "optionalInputs": [
        "readOnlyTemplate",
        "template"
      ],
      "outputs": [],
      "typeScriptMetadata": {
        "ParametersTypeExpression": {
          "TypeExpression": "HubsExtension.TemplateEditorBladeV2.Parameters"
        },
        "ReturnedDataTypeExpression": {
          "TypeExpression": "HubsExtension.TemplateEditorBladeV2.Results"
        }
      }
    },
    {
      "name": "InProductFeedbackBlade",
      "keyParameters": [],
      "inputs": [
        "bladeName",
        "cesQuestion",
        "cvaQuestion",
        "extensionName",
        "featureName",
        "surveyId"
      ],
      "optionalInputs": [],
      "outputs": [],
      "typeScriptMetadata": {
        "ParametersTypeExpression": {
          "TypeExpression": "HubsExtension.InProductFeedbackBlade.Parameters"
        },
        "ReturnedDataTypeExpression": null
      }
    },
    {
      "name": "DeploymentDetailsBlade",
      "keyParameters": [],
      "inputs": [
        "id"
      ],
      "optionalInputs": [
        "packageId",
        "packageIconUri",
        "primaryResourceId",
        "provisioningHash",
        "createBlade"
      ],
      "outputs": [],
      "typeScriptMetadata": {
        "ParametersTypeExpression": {
          "TypeExpression": "HubsExtension.DeploymentDetailsMenuBlade.Parameters"
        },
        "ReturnedDataTypeExpression": null
      }
    },
    {
      "name": "ResourceGroupMapBlade",
      "keyParameters": [
        "id"
      ],
      "inputs": [
        "id"
      ],
      "optionalInputs": [],
      "outputs": [],
      "typeScriptMetadata": {
        "ParametersTypeExpression": {
          "TypeExpression": "HubsExtension.ResourceGroupMapBlade.Parameters"
        },
        "ReturnedDataTypeExpression": null
      }
    },
    {
      "name": "ResourceTagsBlade",
      "keyParameters": [],
      "inputs": [
        "resourceId"
      ],
      "optionalInputs": [],
      "outputs": [],
      "typeScriptMetadata": {
        "ParametersTypeExpression": {
          "TypeExpression": "HubsExtension.ResourceTagsBlade.Parameters"
        },
        "ReturnedDataTypeExpression": null
      }
    },
    {
      "name": "TagsBlade",
      "keyParameters": [],
      "inputs": [],
      "optionalInputs": [
        "subscriptionId"
      ],
      "outputs": [],
      "typeScriptMetadata": {
        "ParametersTypeExpression": {
          "TypeExpression": "HubsExtension.TagsBlade.Parameters"
        },
        "ReturnedDataTypeExpression": null
      }
    },
    {
      "name": "AssignTagsBlade",
      "keyParameters": [],
      "inputs": [
        "resources"
      ],
      "optionalInputs": [
        "tags"
      ],
      "outputs": [],
      "typeScriptMetadata": {
        "ParametersTypeExpression": {
          "TypeExpression": "HubsExtension.AssignTagsBlade.Parameters"
        },
        "ReturnedDataTypeExpression": {
          "TypeExpression": "HubsExtension.AssignTagsBlade.Results"
        }
      }
    },
    {
      "name": "EditTagsBlade",
      "keyParameters": [],
      "inputs": [
        "resource"
      ],
      "optionalInputs": [
        "tagName"
      ],
      "outputs": [],
      "typeScriptMetadata": {
        "ParametersTypeExpression": {
          "TypeExpression": "HubsExtension.EditTagsBlade.Parameters"
        },
        "ReturnedDataTypeExpression": {
          "TypeExpression": "HubsExtension.EditTagsBlade.Results"
        }
      }
    },
    {
      "name": "DeploymentsList.ReactView",
      "keyParameters": [],
      "inputs": [
        "resourceGroup"
      ],
      "optionalInputs": [],
      "outputs": [],
      "typeScriptMetadata": {
        "ParametersTypeExpression": {
          "TypeExpression": "{ resourceGroup: string; }"
        },
        "ReturnedDataTypeExpression": null
      }
    },
    {
      "name": "MyAccess.ReactView",
      "keyParameters": [],
      "inputs": [
        "resourceId"
      ],
      "optionalInputs": [],
      "outputs": [],
      "typeScriptMetadata": {
        "ParametersTypeExpression": {
          "TypeExpression": "{ resourceId: string; }"
        },
        "ReturnedDataTypeExpression": null
      }
    },
    {
      "name": "RPStatus.ReactView",
      "keyParameters": [],
      "inputs": [
        "subscriptionId"
      ],
      "optionalInputs": [],
      "outputs": [],
      "typeScriptMetadata": {
        "ParametersTypeExpression": {
          "TypeExpression": "{ subscriptionId: string; }"
        },
        "ReturnedDataTypeExpression": null
      }
    },
    {
      "name": "ResourcesWithTag.ReactView",
      "keyParameters": [],
      "inputs": [
        "tagName",
        "tagValue"
      ],
      "optionalInputs": [],
      "outputs": [],
      "typeScriptMetadata": {
        "ParametersTypeExpression": {
          "TypeExpression": "{ tagName: string; tagValue: string; }"
        },
        "ReturnedDataTypeExpression": null
      }
    },
    {
      "name": "Tags.ReactView",
      "keyParameters": [],
      "inputs": [],
      "optionalInputs": [],
      "outputs": [],
      "typeScriptMetadata": {
        "ParametersTypeExpression": {
          "TypeExpression": "void"
        },
        "ReturnedDataTypeExpression": null
      }
    },
    {
      "name": "UnauthorizedAssetBlade",
      "keyParameters": [],
      "inputs": [],
      "optionalInputs": [
        "id"
      ],
      "outputs": []
    },
    {
      "name": "NotFoundAssetBlade",
      "keyParameters": [],
      "inputs": [],
      "optionalInputs": [
        "id"
      ],
      "outputs": []
    },
    {
      "name": "UnavailableAssetBlade",
      "keyParameters": [],
      "inputs": [],
      "optionalInputs": [
        "id"
      ],
      "outputs": []
    },
    {
      "name": "Resources",
      "keyParameters": [],
      "inputs": [],
      "optionalInputs": [
        "resourceType",
        "selectedSubscriptionId",
        "filter",
        "scope",
        "kind"
      ],
      "outputs": []
    },
    {
      "name": "BrowseAllResourcesBlade",
      "keyParameters": [],
      "inputs": [],
      "optionalInputs": [
        "resourceType",
        "selectedSubscriptionId",
        "filter",
        "scope",
        "kind"
      ],
      "outputs": []
    },
    {
      "name": "BrowseAllInMenu",
      "keyParameters": [],
      "inputs": [
        "resourceType"
      ],
      "optionalInputs": [
        "scope"
      ],
      "outputs": []
    },
    {
      "name": "BrowseResourceBlade",
      "keyParameters": [],
      "inputs": [],
      "optionalInputs": [
        "resourceType",
        "selectedSubscriptionId",
        "filter",
        "scope",
        "kind"
      ],
      "outputs": []
    },
    {
      "name": "BrowseInMenu",
      "keyParameters": [],
      "inputs": [
        "resourceType"
      ],
      "optionalInputs": [
        "scope",
        "kind"
      ],
      "outputs": []
    },
    {
      "name": "BrowseInstanceLinkBlade",
      "keyParameters": [],
      "inputs": [],
      "optionalInputs": [
        "resourceType",
        "selectedSubscriptionId",
        "filter",
        "scope",
        "kind"
      ],
      "outputs": []
    },
    {
      "name": "BrowseResourceGroupBlade",
      "keyParameters": [],
      "inputs": [],
      "optionalInputs": [
        "resourceType",
        "selectedSubscriptionId",
        "filter",
        "scope",
        "kind"
      ],
      "outputs": []
    },
    {
      "name": "BrowseGroupsInMenu",
      "keyParameters": [],
      "inputs": [
        "resourceType"
      ],
      "optionalInputs": [
        "scope"
      ],
      "outputs": []
    },
    {
      "name": "MapResourceGroupBlade",
      "keyParameters": [],
      "inputs": [
        "id"
      ],
      "optionalInputs": [],
      "outputs": []
    },
    {
      "name": "ResourceGroupPickerV3Blade",
      "keyParameters": [],
      "inputs": [],
      "optionalInputs": [],
      "outputs": [],
      "parameterProvider": true
    },
    {
      "name": "DeployToAzure",
      "keyParameters": [],
      "inputs": [],
      "optionalInputs": [],
      "outputs": [],
      "parameterProvider": true
    },
    {
      "name": "DeployFromTemplateBlade",
      "keyParameters": [],
      "inputs": [
        "internal_bladeCallerParams"
      ],
      "optionalInputs": [],
      "outputs": [],
      "parameterProvider": true
    },
    {
      "name": "ParametersEditorBlade",
      "keyParameters": [],
      "inputs": [],
      "optionalInputs": [],
      "outputs": [],
      "parameterProvider": true
    },
    {
      "name": "ParametersFileEditorBlade",
      "keyParameters": [],
      "inputs": [],
      "optionalInputs": [],
      "outputs": [],
      "parameterProvider": true
    },
    {
      "name": "TemplateEditorBlade",
      "keyParameters": [],
      "inputs": [],
      "optionalInputs": [],
      "outputs": [],
      "parameterProvider": true
    },
    {
      "name": "LocationPickerV3Blade",
      "keyParameters": [],
      "inputs": [],
      "optionalInputs": [],
      "outputs": [],
      "parameterProvider": true
    },
    {
      "name": "DeploymentInputsBlade",
      "keyParameters": [
        "id"
      ],
      "inputs": [
        "id"
      ],
      "optionalInputs": [
        "referrerInfo"
      ],
      "outputs": []
    },
    {
      "name": "DeploymentOutputsBlade",
      "keyParameters": [
        "id"
      ],
      "inputs": [
        "id"
      ],
      "optionalInputs": [
        "referrerInfo"
      ],
      "outputs": []
    },
    {
      "name": "ResourceMenuBlade",
      "keyParameters": [],
      "inputs": [
        "id"
      ],
      "optionalInputs": [
        "menuid",
        "menucontext",
        "referrerInfo"
      ],
      "outputs": []
    },
    {
      "name": "SubscriptionPickerV3Blade",
      "keyParameters": [],
      "inputs": [],
      "optionalInputs": [],
      "outputs": [],
      "parameterProvider": true
    }
  ],
  "bladeTypeScriptDependencies": {
    "DefinitionFileNames": [
      {
        "FileName": "HubsExtension.d.ts"
      }
    ],
    "Modules": null
  },
  "commands": [
    {
      "name": "MoveResourceCommand",
      "inputs": [
        "resourceId"
      ]
    }
  ],
  "controls": []
}