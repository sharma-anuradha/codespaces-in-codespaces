import { EnvironmentType } from 'vso-client-core';

import { ISku } from './ISku';

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

export interface EnvPersonalization {
    dotfilesRepository?: string;
    dotfilesTargetPath?: string;
    dotfilesInstallCommand?: string;
}

export interface EnvironmentSettingsAllowedUpdates {
    allowedAutoShutdownDelayMinutes: number[];
    allowedSkus: ISku[];
}

export interface EnvironmentSettingsUpdate {
    autoShutdownDelayMinutes?: number;
    skuName?: string;
}
