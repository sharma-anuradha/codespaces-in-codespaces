
export class ComponentNames {
    public baseName: string;
    public baseFileName: string;
    public readonly prefix: string;
    public readonly component: string;
    public readonly baseComponentName: string;
    public readonly baseComponentFileName: string;

    constructor (prefix: string, component: string) {
        this.prefix = prefix;
        this.component = component;
        const baseComponentNames = ResourceNames.makeResourceNames(prefix, component);
        this.baseName = this.baseComponentName = baseComponentNames.resourceName;;
        this.baseFileName = this.baseComponentFileName = baseComponentNames.fileName;
    }
}

export class EnvironmentNames extends ComponentNames {
    public readonly env: string;
    public readonly baseEnvironmentName: string;
    public readonly baseEnvironmentFileName: string;

    constructor (componentNames: ComponentNames, env: string) {
        super(componentNames.prefix, componentNames.component);
        this.env = env;
        const baseEnvironmentNames = ResourceNames.makeResourceNames(this.prefix, this.component, this.env);
        this.baseName = this.baseEnvironmentName = baseEnvironmentNames.resourceName;
        this.baseFileName = this.baseEnvironmentFileName = baseEnvironmentNames.fileName;
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
        const basePlaneNames = ResourceNames.makeResourceNames(this.prefix, this.component, this.env, this.plane);
        this.baseName = this.basePlaneName = basePlaneNames.resourceName;
        this.baseFileName = this.basePlaneFileName = basePlaneNames.fileName;
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
        const baseInstanceNames = ResourceNames.makeResourceNames(this.prefix, this.component, this.env, this.plane, this.instance);
        this.baseName = this.baseInstanceName = baseInstanceNames.resourceName;
        this.baseFileName = this.baseInstanceFileName = baseInstanceNames.fileName;
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
        const baseRegionNames = ResourceNames.makeResourceNames(this.prefix, this.component, this.env, this.plane, this.instance, this.geo, this.regionSuffix);
        this.baseName = this.baseRegionName = baseRegionNames.resourceName;
        this.baseRegionStorageName = baseRegionNames.storageName;
        this.baseFileName = this.baseRegionFileName = baseRegionNames.fileName;
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

    public static makeResourceNames(
        prefix: string,
        component: string,
        env: string = null,
        plane: string = null,
        instance: string = null,
        geography: string = null,
        regionSuffix: string = null,
        typeSuffix: string = null
        ): { resourceName:string; storageName:string; fileName:string } {

        let resourceName = this.makeResourceName(prefix, component, env, plane, instance, geography, regionSuffix, typeSuffix);
        const fileName = this.makeBaseFileName(resourceName, prefix, component);

        if (plane === "ctl") {
            resourceName = resourceName.replace('-ctl','');
        }

        const storageName = this.convertToStorageResourceName(prefix, resourceName);

        return {
            resourceName: resourceName,
            storageName: storageName,
            fileName: fileName
        };
    }

    private static makeBaseFileName(resourceName: string, prefix: string, component: string): string {
        const baseComponentName = `${prefix}-${component}`;
        if (resourceName === baseComponentName) {
            return component;
        }
        else {
            const pattern = new RegExp(`${baseComponentName}-`, 'gi');
            return resourceName.replace(pattern, `${component}.`)
        }
    }

    private static makeResourceName(
        prefix: string,
        component: string,
        env: string = null,
        plane: string = null,
        instance: string = null,
        geography: string = null,
        regionSuffix: string = null,
        typeSuffix: string = null
    ): string {

        if (!prefix) {
            throw "prefix is required";
        }

        if (!component) {
            throw "component is required";
        }

        let resourceName = `${prefix}-${component}`;

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

    private static convertToStorageResourceName(prefix: string, baseResourceName: string): string {
        return baseResourceName.replace(`${prefix}-`, '').replace(/-/g, '');
    }
}