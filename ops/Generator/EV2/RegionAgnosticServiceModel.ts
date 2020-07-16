/** The document representation of the generic service modeling of an Azure Service. */
export interface IRegionAgnosticServiceModel {
    /** The version of the schema that a document conforms to. */
    contentVersion: string;
    /** An entity that contains information that can be used to uniquely identify an Azure service. */
    serviceMetadata: IServiceMetadata;
    /** The information on how to create and configure subscriptions. */
    subscriptionProvisioning: ISubscriptionProvisioning;
    serviceResourceGroupDefinitions: IServiceResourceGroupDefinitions[];
}

export interface IServiceMetadata {
    /** The service tree identifier for the Azure service. */
    serviceIdentifier: string;
    /** The human-readable name of this Azure service. If multiple services share the same service tree identifier, serviceGroup can be used to differentiate individual services. */
    serviceGroup: string;
    /** The name to be used for displaying information about the Azure service. */
    displayName: string;
    /** The path to the service specification file. This enables automated registration of a service if not already registered, when using client tools. */
    serviceSpecificationPath: string;
    /** The environment that this particular service is operating in. */
    environment: ServiceMetadataEnvironment;
    /** The properties that corresponds to the build-out of the service. */
    buildout: IBuildout;
}

export interface ISubscriptionProvisioning {
    /** The path to the rollout parameters file that defines the parameters for provisioning subscriptions. */
    rolloutParametersPath: string;
    /** The role assignment configurations for the subscriptions. */
    roleAssignment: IRoleAssignment;
    /** The list of scope tags for subscription provisioning parameters files */
    scopeTags: IScopeTags[];
}

/** The enumeration of the various resource group definitions that represent how to construct the resource groups that constitute this cloud service. */
export interface IServiceResourceGroupDefinitions {
    /** The human-readable name of the definition. */
    name: string;
    /** The key to refer to a specific subscription. Declaring the same subscriptionKey across different ServiceResourceGroupDefinitions will deploy those resources to the same subscription. */
    subscriptionKey: string;
    /** The constraint defining the scope at which the service resource group and the resources defined should be deployed. */
    executionConstraint: ServiceResourceGroupDefinitionsExecutionConstraint;
    /** The list of scope tags for the service resource group definition. */
    scopeTags: IScopeTags[];
    /** The policy information. */
    policy: IPolicy;
    /** The enumeration of the various resource definitions that represent how to construct the resources that constitute this resource group definition. */
    serviceResourceDefinitions: IServiceResourceDefinitions[];
}

export enum ServiceMetadataEnvironment {
    Prod = <any>"Prod",
    Test = <any>"Test",
    Dev = <any>"Dev",
}

export interface IBuildout {
    /** The list of dependent services that should be available before this service can be built in an Azure region. */
    dependencies: IDependencies[];
}

export interface IRoleAssignment {
    /** The path to the ARM template file which declares role assignments that are required to be configured on the subscription. */
    armTemplatePath: string;
    /** The path to the ARM parameters file that corresponds to the role assignments ARM template. */
    armParametersPath: string;
}

/** Defines the scope tag */
export interface IScopeTags {
    /** The scope tag name */
    name: string;
}

export enum ServiceResourceGroupDefinitionsExecutionConstraint {
    OncePerCloud = <any>"OncePerCloud",
    OncePerRegion = <any>"OncePerRegion",
    OncePerStamp = <any>"OncePerStamp",
}

export interface IPolicy {
    /** Determines if safe rollout policy should be applied on this resource group definition. */
    skipSafeRolloutPolicyCheck: boolean;
}

/** The object representation of the definition of a particular resource in the Cloud Service Model. */
export interface IServiceResourceDefinitions {
    /** The human-readable name of the definition. */
    name: string;
    /** The list of scope tags for the service resource definition. */
    scopeTags: IScopeTags[];
    /** The policy information. */
    policy: IPolicy;
    /** Resource composition parts which apply to this resource definition */
    composedOf: IComposedOf;
}

/** The information that identifies the dependency. */
export interface IDependencies {
    /** The name given to refer to this dependency. */
    name: string;
    /** The service tree identifier of the dependency. */
    serviceIdentifier: string;
    /** The name to be used to display information about this dependency. */
    displayName: string;
}

export interface IComposedOf {
    /** Arm Composition Part */
    arm: IArm;
    /** Extension Composition Part */
    extension: IExtension;
}


export interface IArm {
    /** The path to the ARM template file for this particular definition. */
    templatePath: string;
    /** The path to the ARM parameters file for this particular definition. */
    parametersPath: string;
}

export interface IExtension {
    /** The path to the rollout parameters file for this particular definition. */
    rolloutParametersPath: string;
    /** HTTP extensions. */
    http: IHttp[];
    /** Shell extensions. */
    shell: IShell[];
}

export interface IHttp {
    /** Full extension name, e.g. 'ExtenionNamespace/ExtentionType' */
    type: string;
}

export interface IShell {
    /** The shell extension name, e.g. 'MySimpleShell' */
    type: string;
}