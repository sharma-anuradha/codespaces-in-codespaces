
export class ComponentNames {
    public baseName: string;
    public readonly prefix: string;
    public readonly component: string;
    public readonly baseComponentName: string;
    public readonly baseComponentFileName: string;

    constructor (prefix: string, component: string) {
        this.prefix = prefix;
        this.component = component;
        this.baseComponentName = ResourceNames.makeResourceName(prefix, component);
        this.baseName = this.baseComponentName;
        this.baseComponentFileName = ResourceNames.makeBaseFileName(this.baseName, prefix, component);
    }
}

export class EnvironmentNames extends ComponentNames {
    public readonly env: string;
    public readonly baseEnvironmentName: string;
    public readonly baseEnvironmentFileName: string;

    constructor (componentNames: ComponentNames, env: string) {
        super(componentNames.prefix, componentNames.component);
        this.env = env;
        this.baseEnvironmentName = ResourceNames.makeResourceName(this.prefix, this.component, this.env);
        this.baseName = this.baseEnvironmentName;
        this.baseEnvironmentFileName = ResourceNames.makeBaseFileName(this.baseEnvironmentName, this.prefix, this.component);
    }
}

export class PlaneNames extends EnvironmentNames {
    public readonly plane: string;
    public readonly basePlaneName: string;
    public readonly basePlaneFileName: string;
    public readonly subscriptionName: string;
    public readonly subscriptionId: string;

    constructor (environmentNames: EnvironmentNames, plane: string, subscriptionName: string, subscriptionId: string) {
        super(environmentNames, environmentNames.env);
        this.plane = plane;
        this.subscriptionName = subscriptionName;
        this.subscriptionId = subscriptionId;
        this.basePlaneName = ResourceNames.makeResourceName(this.prefix, this.component, this.env, this.plane);
        this.baseName = this.basePlaneName;
        this.basePlaneFileName = ResourceNames.makeBaseFileName(this.basePlaneName, this.prefix, this.component);
    }
}

export class InstanceNames extends PlaneNames {
    public readonly instance: string;
    public readonly instanceLocation: string;
    public readonly instanceRegions: string[];
    public readonly baseInstanceName: string;
    public readonly baseInstanceFileName: string;

    constructor(planeNames: PlaneNames, instance: string, instanceLocation: string, instanceRegions: string[]) {
        super(planeNames, planeNames.plane, planeNames.subscriptionName, planeNames.subscriptionId);
        this.instance = instance;
        this.instanceLocation = instanceLocation;
        this.instanceRegions = instanceRegions;
        this.baseInstanceName = ResourceNames.makeResourceName(this.prefix, this.component, this.env, this.plane, this.instance);
        this.baseName = this.baseInstanceName;
        this.baseInstanceFileName = ResourceNames.makeBaseFileName(this.baseInstanceName, this.prefix, this.component);
    }
}

export class RegionNames extends InstanceNames {
    public readonly region: string;
    public readonly geo: string;
    public readonly regionSuffix: string;
    public readonly regionLocation: string;
    public readonly baseRegionName: string;
    public readonly baseRegionFileName: string;
    public readonly baseRegionStorageName: string;
    constructor(instanceNames: InstanceNames, region:string, geo: string, regionSuffix: string, regionLocation: string) {
        super(instanceNames, instanceNames.instance, instanceNames.instanceLocation, instanceNames.instanceRegions);
        this.region = region;
        this.geo = geo;
        this.regionSuffix = regionSuffix;
        this.regionLocation = regionLocation;
        this.baseRegionName = ResourceNames.makeResourceName(this.prefix, this.component, this.env, this.plane, this.instance, this.geo, this.regionSuffix);
        this.baseName = this.baseRegionName;
        this.baseRegionStorageName = ResourceNames.convertToStorageResourceName(this.prefix, this.baseRegionName);
        this.baseRegionFileName = ResourceNames.makeBaseFileName(this.baseRegionName, this.prefix, this.component);
    }
}

export default abstract class ResourceNames {

    public static sortKeys<T>(obj_1: T): T {
        const key = Object.keys(obj_1)
            .sort(function order(key1, key2) {
                if (key1 < key2) return -1;
                else if (key1 > key2) return +1;
                else return 0;
            });

        // Taking the object in 'temp' object
        // and deleting the original object.
        const temp = {};

        for (let i = 0; i < key.length; i++) {
            temp[key[i]] = obj_1[key[i]];
            delete obj_1[key[i]];
        }

        // Copying the object from 'temp' to
        // 'original object'.
        for (let i = 0; i < key.length; i++) {
            obj_1[key[i]] = temp[key[i]];
        }
        return obj_1;
    }

    public static makeBaseFileName(resourceName: string, globalPrefix: string, component: string): string {
        const baseComponentName = `${globalPrefix}-${component}`;
        if (resourceName === baseComponentName) {
            return component;
        }
        else {
            const pattern = new RegExp(`${baseComponentName}-`, 'gi');
            return resourceName.replace(pattern, `${component}.`)
        }
    }

    public static makeResourceName(
        globalPrefix: string,
        component: string,
        env: string = null,
        plane: string = null,
        instance: string = null,
        geography: string = null,
        regionSuffix: string = null,
        typeSuffix: string = null
    ): string {

        if (!globalPrefix) {
            throw "globalPrefix is required";
        }

        if (!component) {
            throw "component is required";
        }

        let resourceName = `${globalPrefix}-${component}`;

        if (env) {
            resourceName += `-${env}`;

            if (plane) {
                resourceName += `-${plane}`;

                if (instance) {
                    resourceName += `-${instance}`;
                }

                if (geography) {
                    resourceName += `-${geography}`;
                    if (regionSuffix) {
                        resourceName += `-${regionSuffix}`;
                    }
                }

                if (typeSuffix) {
                    resourceName += `-${typeSuffix}`;
                }
            }
        }

        return resourceName;
    }

    public static convertToStorageResourceName(prefix: string, baseResourceName: string): string {
        return baseResourceName.replace(`${prefix}-`, '').replace(/-/g, '');
    }
}