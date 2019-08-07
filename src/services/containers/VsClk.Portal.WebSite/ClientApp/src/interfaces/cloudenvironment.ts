export interface CreateEnvironmentParameters {
    type?: 'cloudEnvironment' | 'staticEnvironment';
    friendlyName: string;
    gitRepositoryUrl?: string;
}

export enum StateInfo {
    Provisioning = 'Provisioning',
    Deleted = 'Deleted',
    Available = 'Available',
    Unavailable = 'Unavailable',
    Hibernating = 'Hibernating',
    WakingUp = 'WakingUp',
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
}

type RequiredLocalEnvironmentProperties = 'state' | 'seed' | 'friendlyName' | 'created' | 'updated';
export type ILocalCloudEnvironment = { lieId?: string } & Partial<
    Omit<ICloudEnvironment, RequiredLocalEnvironmentProperties>
> &
    Pick<ICloudEnvironment, RequiredLocalEnvironmentProperties>;

export function environmentIsALie({ lieId }: ILocalCloudEnvironment) {
    return lieId != null;
}

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
