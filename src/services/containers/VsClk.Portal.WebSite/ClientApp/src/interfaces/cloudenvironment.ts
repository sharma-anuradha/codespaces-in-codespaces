export interface CreateEnvironmentParameters {
    type?: EnvironmentType;
    friendlyName: string;
    planId: string;
    location: string;
    gitRepositoryUrl?: string;
    userName: string;
    userEmail: string;
    dotfilesRepository?: string;
    dotfilesTargetPath?: string;
    dotfilesInstallCommand?: string;
    autoShutdownDelayMinutes: number;
    skuName: string;
}

export enum StateInfo {
    Deleted = 'Deleted',
    Available = 'Available',
    Unavailable = 'Unavailable',
    Shutdown = 'Shutdown',
    ShuttingDown = 'ShuttingDown',
    Failed = 'Failed',
    Starting = 'Starting',
    Provisioning = 'Provisioning',
}

export enum EnvironmentType {
    CloudEnvironment = 'CloudEnvironment',
    StaticEnvironment = 'StaticEnvironment',
}

export interface ICloudEnvironment {
    id: string;
    type: EnvironmentType;
    friendlyName: string;
    state: StateInfo;
    seed: Seed;
    connection: Connection;
    created: Date;
    updated: Date;
    personalization?: EnvPersonalization;
    planId: string;
    location: string;
    skuName: string;
    skuDisplayName: string;
    autoShutdownDelayMinutes?: number;
    lastStateUpdateReason?: string;
}

export interface EnvPersonalization {
    dotfilesRepository?: string;
    dotfilesTargetPath?: string;
    dotfilesInstallCommand?: string;
}

type RequiredLocalEnvironmentProperties =
    | 'state'
    | 'seed'
    | 'friendlyName'
    | 'created'
    | 'updated'
    | 'skuName';
export type ILocalCloudEnvironment = { lieId?: string } & Partial<
    Omit<ICloudEnvironment, RequiredLocalEnvironmentProperties>
> &
    Pick<ICloudEnvironment, RequiredLocalEnvironmentProperties>;

export interface Connection {
    sessionId: string;
    sessionPath: string;
}
export interface Seed {
    moniker: string;
    type: string;
}

export interface GitConfig {
    userName?: string;
    userEmail?: string;
}

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
}
