export class Codespace {
    readonly type: string = 'CloudEnvironment';
    id: string;
    friendlyName: string;
    state: string;
    planId: string;
    location: string;
    skuName: string;
    seed: Seed;
    personalization?: Personalization;
    autoShutdownDelayMinutes: number;
}

export class Seed {
    type: string;
    gitConfig?: GitConfig;
    moniker?: string;
    recurseClone?: boolean;
}

class Personalization {
    dotfilesRepository: string;
    dotfilesTargetPath: string;
    dotfilesInstallCommand: string;
}

interface GitConfig {
    userName?: string;
    userEmail?: string;
}

export class Location {
    skus: Sku[];
    defaultAutoSuspendDelayMinutes: number[];
}

export class Sku {
    name: string;
    displayName: string;
    os: string;
}

export const startingLower = 'starting';
export const provisioningLower = 'provisioning';
export const shuttingDownLower = 'shuttingdown';
export const suspendedLower = 'suspended';
export const shutdownLower = 'shutdown';
export const availableLower = 'available';
export function isTransient(codespace: Codespace): boolean {
    const stateLower = codespace.state.toLowerCase();
    return stateLower === provisioningLower || stateLower === shuttingDownLower;
}
