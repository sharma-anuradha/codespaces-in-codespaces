export interface CreateEnvironmentParameters {
    type?: 'cloudEnvironment' | 'staticEnvironment';
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
}

export enum StateInfo {
    Creating = 'Creating',
    Deleted = 'Deleted',
    Available = 'Available',
    Unavailable = 'Unavailable',
    Shutdown = 'Shutdown',
    ShuttingDown = 'Shutting Down',
    Failed = 'Failed to Create',
    Starting = 'Starting',
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
    planId?: string;
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
