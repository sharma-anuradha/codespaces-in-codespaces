export interface IEnvironmentPersonalization {
    readonly dotfilesRepository?: string;
    readonly dotfilesTargetPath?: string;
    readonly dotfilesInstallCommand?: string;
}

export interface IEnvironmentSeed {
    readonly moniker: string;
    readonly type: string;
}

export interface CreateEnvironmentParameters {
    readonly type?: EnvironmentType;
    readonly friendlyName: string;
    readonly planId: string;
    readonly location: string;
    readonly gitRepositoryUrl?: string;
    readonly userName: string;
    readonly userEmail: string;
    readonly dotfilesRepository?: string;
    readonly dotfilesTargetPath?: string;
    readonly dotfilesInstallCommand?: string;
    readonly autoShutdownDelayMinutes: number;
    readonly skuName: string;
}

export enum EnvironmentStateInfo {
    Deleted = 'Deleted',
    Available = 'Available',
    Unavailable = 'Unavailable',
    Shutdown = 'Shutdown',
    ShuttingDown = 'ShuttingDown',
    Failed = 'Failed',
    Starting = 'Starting',
    Provisioning = 'Provisioning',
    Queued = "Queued"
}

export enum EnvironmentType {
    CloudEnvironment = 'CloudEnvironment',
    StaticEnvironment = 'StaticEnvironment',
}

export interface IEnvironmentConnection {
    readonly sessionId: string;
    readonly sessionPath: string;
}

export interface IEnvironment {
    readonly id: string;
    readonly type: EnvironmentType;
    readonly friendlyName: string;
    readonly state: EnvironmentStateInfo;
    readonly seed: IEnvironmentSeed;
    readonly connection: IEnvironmentConnection;
    readonly created: Date;
    readonly updated: Date;
    readonly personalization?: IEnvironmentPersonalization;
    readonly planId: string;
    readonly location: string;
    readonly skuName: string;
    readonly skuDisplayName: string;
    readonly autoShutdownDelayMinutes?: number;
    readonly lastStateUpdateReason?: string;
}

type RequiredLocalEnvironmentProperties =
    | 'state'
    | 'seed'
    | 'friendlyName'
    | 'created'
    | 'updated'
    | 'skuName';
    
export type ILocalEnvironment = { lieId?: string } & Partial<
    Omit<IEnvironment, RequiredLocalEnvironmentProperties>
> &
    Pick<IEnvironment, RequiredLocalEnvironmentProperties>;


export enum EnvironmentErrorCodes {
    unknown = 0,
    exceededQuota = 1,
    environmentNameAlreadyExists = 2,
    environmentDoesNotExist = 3,
    shutdownStaticEnvironment = 4,
    startStaticEnvironment = 5,
    environmentNotAvailable = 6,
    environmentNotShutdown = 7,
    unableToAllocateResources = 8,
    unableToAllocateResourcesWhileStarting = 9,
    requestedAutoShutdownDelayMinutesIsInvalid = 10,
    unableToUpdateSku = 11,
    requestedSkuIsInvalid = 12,
    heartbeatUnhealthy = 13,
    customContainersCreationFailed = 14,
    // CLI MESSAGES

    shutdownFailed = 1001,
    cMBMutexFailure = 1002,
    cMBGeneralError = 1003,
    startEnvironmentHandlerFailedToStartContainer = 1004,
    startEnvironmentHandlerRequiredParameterMissing = 1005,
    startEnvironmentHandlerKitchensinkMissing = 1006,
    startEnvironmentHandlerLiveshareLoginFailed = 1007,
    startEnvironmentHandlerMoreThanOneContainerFoundOnRestart = 1008,
    customContainersGeneralError = 1100,
    customContainersKitchensinkCreationFailed = 1121,
    customContainersKitchensinkStartFailed = 1122,
    customContainersCloneFailed = 1151,
    customContainersPrivateClonetimeout = 1152,
    customContainersCouldNotPullImage = 1153,
    customContainersCouldNotBuildUserImage = 1154,
    customContainersCouldNotCreateUserContainer = 1155,
    customContainersCouldNotRunUserContainer = 1156,
    customContainersCLICopyFailed = 1157,
    customContainersDependenciesFailed = 1158,
    customContainersCLIStartFailed = 1158,
    customContainersIncorrectUserID = 1159,

    customContainersComposeGeneralError = 1200,
    customContainersComposeValidationError = 1201,
    customContainersComposeConfigError = 1202,
    customContainersWrongServiceError = 1203,
    customContainersComposeUpError = 1204,
}