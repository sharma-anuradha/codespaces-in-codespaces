/** A document that declares what actions are to be taken as part of an update to an Azure Service. */
export interface IRolloutSpecification {
    /** The version of the schema that a document conforms to. */
    contentVersion: string;
    /** The metadata associated with this particular rollout. */
    rolloutMetadata: IRolloutMetadata;
    /** The exact sequence of steps that must be executed as part of this rollout.  */
    orchestratedSteps: IOrchestratedSteps[];
}

export interface IRolloutMetadata {
    /** The path relative to the Service Group Root that points to the generic service model of the service that is being updated as part of this rollout. */
    serviceModelPath: string;
    /** The path relative to the Service Group Root that points to the scope bindings file. */
    scopeBindingsPath: string;
    /** The user-specified name of this particular rollout. */
    name: string;
    /** The scope of this particular rollout. */
    rolloutType: RolloutMetadataRolloutType;
    /** The location of the build to use for this particular rollout. */
    buildSource: IBuildSource;
    /** Notification definitions */
    notification: INotification;
    /** List of rollout policy references to use for the rollout. */
    rolloutPolicyReferences: IRolloutPolicyReferences[];
}

/** An individual deployment step in the rollout of an Azure service. */
export interface IOrchestratedSteps {
    /** The name of the rollout step. */
    name: string;
    /** The type of the intended target of this rollout. */
    targetType: string;
    /** The unique identifier of the target that is to be updated. */
    targetName: string;
    /** The actions that must take place as part of this step. The actions will be executed in the order that they are declared. The action names must be unique. If this is an Extension action, the name of the extension must exist in the 'Extensions' block in  RolloutParameters. */
    actions: string[];
    /** The names of the rollout steps that must be executed prior to the current step being executed. */
    dependsOn: string[];
}

export enum RolloutMetadataRolloutType {
    Major = <any>"Major",
    Minor = <any>"Minor",
    Hotfix = <any>"Hotfix",
}

export interface IBuildSource {
    /** The parameters that define how to access and/or prepare the build from this build source. */
    parameters: IParameters;
}

export interface INotification {
    /** Email Notification definitions */
    email: IEmail;
}

/** Policy reference details. */
export interface IRolloutPolicyReferences {
    /** The name of the policy. */
    name: string;
    /** The version of the policy to use. Specify '*' to use the latest registered version of the policy. */
    version: string;
}

export enum OrchestratedStepsTargetType {
    ServiceResourceDefinition = <any>"ServiceResourceDefinition",
}

export enum Actions {
    Deploy = <any>"Deploy",
    MdmHealthCheck = <any>"MdmHealthCheck",
    Extension__ActionName_ = <any>"Extension/{ActionName}",
}

export interface IParameters {
    /** The path relative to the Service Group Root which points to the file whose contents represent the version of the build being deployed.  */
    versionFile: string;
}

export interface IEmail {
    /** To email addresses list separator with ',;' */
    to: string;
    /** Cc email addresses list separator with ',;' */
    cc?: string;
}