/** The document representation of the Cloud Service Model of an Azure Service. */
export interface IServiceModel {
    /** The version of the schema that a document conforms to. */
    contentVersion: string;
    /** An entity that contains information that can be used to uniquely identify an Azure service. */
    serviceMetadata: IServiceMetadata;
    /** The list of application definitions for application deployment. */
    applicationDefinitions: IApplicationDefinitions[];
    serviceResourceGroupDefinitions: IServiceResourceGroupDefinitions[];
    /** The various resource groups that constitute this service. */
    serviceResourceGroups: IServiceResourceGroups[];
}

export interface IServiceMetadata {
    /** The unique identifier for the service registered with Azure Deployment Manager. */
    serviceIdentifier?: string;
    /** The human-readable name of the current instance of this Azure service. */
    serviceGroup: string;
    /** The environment that this particular service is operating in. */
    environment: ServiceMetadataEnvironment;
}

/** The application defintion. */
export interface IApplicationDefinitions {
    /** The human-readable name of the application definition. */
    name: string;
    /** The AKS application properties. */
    azureKubernetesService: IAzureKubernetesService;
}

/** The enumeration of the various resource group definitions that represent how to construct the resource groups that constitute this cloud service. */
export interface IServiceResourceGroupDefinitions {
    /** The human-readable name of the definition. */
    name: string;
    /** The policy information. */
    policy: IPolicy;
    /** The enumeration of the various resource definitions that represent how to construct the resources that constitute this resource group definition. */
    serviceResourceDefinitions: IServiceResourceDefinitions[];
}

/** The entity representation of a resource group in the Cloud Service Model. */
export interface IServiceResourceGroups {
    /** The string that uniquely identifies a particular resource group. This name must match the actual Resource Group name in the Azure subscription. */
    azureResourceGroupName: string;
    /** The location of the resource group instance. */
    location: string;
    /** The name of the template that defines how to construct this particular resource group. It must be declared in the Templates section of the Service Model.  */
    instanceOf: string;
    /** The Azure Subscription ID that this particular resource group is associated with.  */
    azureSubscriptionId: string;
    /** The list of scope tags for the service resource group. */
    scopeTags: IScopeTags[];
    /** The various resources that constitute this particular resource group. */
    serviceResources: IServiceResources[];
}

export enum ServiceMetadataEnvironment {
    Prod = <any>"Prod",
    Test = <any>"Test",
    Dev = <any>"Dev",
}

export interface IAzureKubernetesService {
    /** The AKS spec path. */
    specPath: string;
    /** The AKS namespace. */
    namespace: string;
}

export interface IPolicy {
    /** Determines if safe rollout policy should be applied on this resource group definition. */
    skipSafeRolloutPolicyCheck: boolean;
}

/** The object representation of the definition of a particular resource in the Cloud Service Model. */
export interface IServiceResourceDefinitions {
    /** The human-readable name of the definition. */
    name: string;
    /** The policy information. */
    policy: IPolicy;
    /** Resource composition parts which apply to this resource definition */
    composedOf: IComposedOf;
}


export interface IScopeTags {
    /** The scope tag name. */
    name: string;
}

/** The entity representation of an individual resource in the Cloud Service Model. */
export interface IServiceResources {
    /** The string that uniquely identifies a particular resource. This must match the actual Resource name in the Azure subscription. */
    name?: string;
    /** The name of the template that defines how to construct this particular resource. It must be declared in the Templates section of the Service Model.  */
    instanceOf?: any;
    /** The path to the entity that contains the Azure Resource Model parameters this resource group requires in order to be deployed. */
    armParametersPath?: string;
    /** The path to the entity that contains the rollout parameters for the resource deployment. */
    rolloutParametersPath?: string;
    /** The list of scope tags for the service resource. */
    scopeTags?: IScopeTags[];
    /** The list of application instances. */
    applications?: IApplications[];
}

export interface IComposedOf {
    /** Arm Composition Part */
    arm: IArm;
    /** Extension Composition Part */
    extension: any;
    /** The application details object. */
    application?: IApplication;
}

/** The details of the application instance */
export interface IApplications {
    /** The name of the application instance. */
    name: string;
    /** The name of the application defintion to which this instance belongs. */
    instanceOf: string;
    /** The AKS cluster name. It is required when using CertificateAuthentication */
    armResourceName: string;
    /** The list of scope tags for the application. */
    scopeTags: IScopeTags[];
}

export interface IArm {
    /** The path to the entity that contains the Azure Resource Model template for this particular definition. */
    templatePath: string;
    /** The path to the entity that contains the parameterized Azure Resource Model parameters this resource requires in order to be deployed. */
    parametersPath: string;
}

export interface IApplication {
    /** The list of application definition names. */
    names: string[];
}

export interface IAllowedTypes {
    /** Full extension name, e.g. 'ExtenionNamespace/ExtentionType' */
    type: string;
}