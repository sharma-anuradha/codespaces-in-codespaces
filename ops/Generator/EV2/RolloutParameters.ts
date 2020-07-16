/** A document that declares the parameters for an update to an Azure Service. */
export interface IRolloutParameters {
    /** The version of the schema that a document conforms to. */
    contentVersion: string;
    /** The list of parameters for the application deployment. */
    applications: IApplications[];
    /** The parameters for the Mdm Health Check action. */
    mdmHealthCheckParameters: IMdmHealthCheckParameters;
    /** List of Extensions and associated name, type and properties. */
    extensions: IExtensions[];
    /** List of shell extensions and associated name, type and properties. */
    shellExtensions: IShellExtensions[];
}

/** The parameters for each application deployment */
export interface IApplications {
    /** The service resource instance name. */
    serviceResourceName: string;
    /** The application instance name. */
    applicationName: string;
    /** The details of authentication information for connecting to cluster. */
    authentication: IAuthentication;
}

export interface IMdmHealthCheckParameters {
    /** The Mdm Health endpoint URL to be used to check the health of the resource. */
    mdmHealthCheckEndPoint: string;
    /** The monitoring account name used to query the health of the resource. */
    monitoringAccountName: string;
    /** The Cloud Service Name to check the health of. This is to be specified only if the ServiceResource name doesn't correspond to the service name to check the health of. */
    cloudServiceName: string;
    /** An integer value representing the time the rollout system waits before checking the health of the resource. */
    waitBeforeMonitorTimeInMinutes: number;
    /** An integer value representing the time the rollout system monitors the health of the resource after the 'WaitBeforeMoniorTimeInMinutes' time has elapsed. */
    monitorTimeInMinutes: number;
    /** A list of resources to check the health of. */
    healthResources: IHealthResources[];
}

/** Extension and associated name, type and properties. */
export interface IExtensions {
    /** Extension name uniquely identifying a specific extension invocation. */
    name: string;
    /** Registered extension type. */
    type: string;
    /** Version of the extension. */
    version: string;
    /** Connection properties of extension request. */
    connectionProperties: IConnectionProperties;
    /** Key-value property bag to send to http extension in the request body */
    payloadProperties: any;
}

/** Shell extension and associated name, type and properties. */
export interface IShellExtensions {
    /** Name of the shell referenced in Orchestrated Step Action in Rollout Specification. */
    name: string;
    /** Type of shell referenced in Service Resource Definition in Service Model. */
    type: string;
    /** Properties for shell execution. */
    properties: IProperties;
    /** The package containing shell scripts. */
    package: IPackage;
    /** The launch parameters for your shell. */
    launch: ILaunch;
}

export interface IAuthentication {
    /** The type of authentication. Valid values are CertificateAuthentication, KubeConfig. */
    type: string;
    /** The properties for authentication. */
    properties: IAuthenticationProperties;
    /** Parameter reference for authentication. Required for kubeconfig authentication. */
    reference: IReference;
}

/** The object representation of the definition of a health resource. */
export interface IHealthResources {
    /** The human-readable name of the health resource. */
    name: string;
    /** The type of the health resource as defined in the Geneva health system (MDM). */
    resourceType: string;
    /** The dimensions that identify the health resource in the Geneva health system (MDM). */
    dimensions: any;
}

export interface IConnectionProperties {
    /** One of the registered endpoints of the extension, this is optional if only one endpoint is registered. */
    endpoint: string;
    /** Maximum execution time for the extension request, in Iso8601 format. If the extension does not complete within the specified duration, the request will be abandoned and the action will be designated as Failed. */
    maxExecutionTime: string;
    /** Authentication information for extension request */
    authentication: IAuthentication;
}

export interface IProperties {
    /** Timeout for shell execution. Shell will be terminated if the max time is reached and the shell has not exited. */
    maxExecutionTime: string;
}

export interface IPackage {
    /** Reference to a path. */
    reference: IReference2;
}

export interface ILaunch {
    /** List of startup commands for your script execution. */
    command: string[];
    /** List of environment variables. */
    environmentVariables: IEnvironmentVariables[];
    /** The mount containing secrets needed for execution. */
    secretVolumes: ISecretVolumes[];
    /** The mount containing files needed for execution. */
    fileVolumes: IFileVolumes[];
    /** The network profile Id required for VNet. */
    networkProfile: INetworkProfile;
    /** The identity referenced required for Managed Identities. */
    identity: IIdentity;
}

export interface IAuthenticationProperties {
    /** The credentials to be used to get kubeconfig. Valid values are: User, Admin */
    aksRole: string;
}

export interface IReference {
    /** Valid value is 'AzureKeyVault' */
    provider: string;
    /** Parameters for provider */
    parameters: IParameters;
}

export interface IReference2 {
    /** Path relative to the service group root that contains the package. */
    path: string;
}

/** The environment variable. */
export interface IEnvironmentVariables {
    /** The environment variable name. */
    name: string;
    /** The plaintext value. */
    value: string;
    /** Reference to a path or secret. */
    reference: IEnvironmentVariablesReference;
    /** Passes the environment variable as secure value to your shell so that it is not visible on Azure Portal/CLI etc. */
    asSecureValue: string;
}

/** The secret definition containing name, reference, etc. */
export interface ISecretVolumes {
    /** The name of the secret. */
    name: string;
    /** The mount path. */
    mountPath: string;
    /** The secret references. */
    secrets: any[];
}

/** The mount file needed for execution. */
export interface IFileVolumes {
    /** The mount name. */
    name: string;
    /** The mount path. */
    mountPath: string;
    /** The Azure storage file details. */
    azureFile: IAzureFile;
}

export interface INetworkProfile {
    /** The network profile Id required for VNet. */
    id: string;
}

export interface IIdentity {
    /** Type of identity e.g. userAssigned. */
    type: string;
    /** The list of user assigned identities. */
    userAssignedIdentities: string[];
}

export interface IParameters {
    /** The Key vault reference to the kubeconfig secret. */
    secretId: string;
}

export interface IEnvironmentVariablesReference {
    /** The path to a file. */
    path: string;
    /** The provider for secret reference. */
    provider: string;
    /** The identifiers for secret reference. */
    parameters: any;
    /** True or False value that determines if Ev2 should look for and replace any scope tags for parameter replacements. */
    enableScopeTagBindings_: string;
}

export interface IAzureFile {
    /** The storage account name. */
    storageAccountName: string;
    /** The storage account keys */
    storageAccountKey: IStorageAccountKey;
}

export interface IStorageAccountKey {
    /** Reference to a secret. */
    reference: IStorageAccountKeyReference;
}

export interface IStorageAccountKeyReference {
    /** The provider for secret reference. */
    provider: string;
    /** The identifiers for secret reference. */
    parameters: any;
}