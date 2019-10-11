export interface CreateEnvironmentParameters {
    type?: 'cloudEnvironment' | 'staticEnvironment';
    friendlyName: string;
    gitRepositoryUrl?: string;
    userName: string;
    userEmail: string;
    dotfilesRepository?: string;
    dotfilesTargetPath?: string;
    dotfilesInstallCommand?: string;
}

export enum StateInfo {
    Provisioning = 'Provisioning',
    Deleted = 'Deleted',
    Available = 'Available',
    Unavailable = 'Unavailable',
    Hibernating = 'Hibernating',
    WakingUp = 'WakingUp',
    Shutdown = 'Shutdown',
    ShuttingDown = 'ShuttingDown',
    Failed = 'Failed to Create',
}

export interface ICloudEnvironment {
    id: string;
    type: 'cloudEnvironment' | 'staticEnvironment';
    friendlyName: string;
    state: StateInfo;
    seed: Seed;
    connection: Connection;
    created: Date;
    updated: Date;
    personalization?: EnvPersonalization;
    accountId?: string;
    location?: string;
}

export interface EnvPersonalization {
    dotfilesRepository?: string;
    dotfilesTargetPath?: string;
    dotfilesInstallCommand?: string;
}

type RequiredLocalEnvironmentProperties = 'state' | 'seed' | 'friendlyName' | 'created' | 'updated';
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
