// ResourceNameDefs

export interface IComponentNames {
    readonly baseName: string;
    readonly baseFileName: string;
    readonly prefix: string;
    readonly component: string;
    readonly componentDisplayName: string;
    readonly baseComponentName: string;
    readonly baseComponentFileName: string;
}

export interface IEnvironmentNames extends IComponentNames {
    readonly env: string;
    readonly baseEnvironmentName: string;
    readonly baseEnvironmentFileName: string;
    readonly environmentLocation: string;
    readonly environmentTenantId: string;
    readonly environmentStampLocations: string[];
    readonly location : string;
}

export interface IPlaneNames extends IEnvironmentNames {
    readonly plane: string;
    readonly basePlaneName: string;
    readonly basePlaneFileName: string;
    readonly subscriptionName: string;
    readonly subscriptionId: string;
}

export interface IInstanceNames extends IPlaneNames {
    readonly instance: string;
    readonly instanceLocation: string;
    readonly instanceStampRegions: string[];
    readonly instanceStampLocations: string[];
    readonly baseInstanceName: string;
    readonly baseInstanceFileName: string;
}

export interface IDataSubscription {
    id: string;
    name: string;
    serviceType: string;
}

export interface IRegionNames extends IInstanceNames {
    readonly region: string;
    readonly geo: string;
    readonly regionSuffix: string;
    readonly regionLocation: string;
    readonly baseRegionName: string;
    readonly baseRegionFileName: string;
    readonly baseRegionStorageName: string;
    readonly regionDataSubscriptions: () => IDataSubscription[];
}
