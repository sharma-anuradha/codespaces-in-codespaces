// Environments.ts

import { find, cloneDeep } from "lodash";
import ResourceNames, { EnvironmentNames, InstanceNames, RegionNames, PlaneNames, ComponentNames } from "./ResourceNames";

export class EnvironmentsDeployment implements IEnvironmentsDeployment {
    globalPrefix: string;
    environments: Environment[] = [];
    geographies: Geography[] = [];

    constructor(environments: Record<string, undefined>) {
        this.parseEnvironmentsDeploymentsJson(environments);
    }

    private getGeography(name: string): Geography {
        return find(this.geographies, ["name", name]);
    }

    private getRegion(geo: Geography, name: string): Region {
        return find(geo.regions, ["name", name]);
    }

    private parseEnvironmentsDeploymentsJson(env: Record<string, undefined>) {
        this.parseGeographiesJson(env.geographies);
        this.parseEnvironmentsJson(env.environments);
    }

    private getDataLocation(location: string): DataLocation {
        const locationSplit = location.split("-");
        const geoTag = this.getGeography(locationSplit[0]);
        const regionTag = this.getRegion(geoTag, locationSplit[1]);
        const dl = new DataLocation();
        dl.geography = geoTag;
        dl.region = regionTag;
        return dl;
    }

    private parseEnvironmentsJson(environments: Record<string, any>) {
        for (const envName in environments) {
            const envObj = environments[envName];
            const envLocation = this.getDataLocation(envObj.location);
            const env = new Environment(envName, envObj.pme, envLocation);
            for (const plane of env.planes) {
                for (const instanceName in envObj.instances) {
                    const instanceObj = envObj.instances[instanceName];
                    const instance = new Instance(instanceName, plane);
                    for (const stampName in instanceObj.stamps) {
                        const stampLocation = this.getDataLocation(instanceObj.stamps[stampName].location);
                        const stamp = new Stamp(stampName, stampLocation);
                        instance.addStamp(stamp);
                    }
                    plane.addInstance(instance);
                }
            }
            this.environments.push(env);
        }
    }

    private parseGeographiesJson(geos: Record<string, Record<string, string>>) {
        for (const geoName in geos) {
            const geo = new Geography(geoName);
            for (const regName in geos[geoName]) {
                const reg = new Region(regName, geos[geoName][regName]);
                geo.addRegion(reg);
            }
            this.geographies.push(geo);
        }
    }
}

export interface IEnvironmentsDeployment {
    environments: Environment[];
    geographies: Geography[];
}

export class Geography {
    readonly name: string;
    regions: Region[] = [];

    constructor(name: string) {
        this.name = name;
    }

    addRegion(region: Region): void {
        this.regions.push(region);
    }
}

export class Region {
    readonly name: string;
    readonly azureLocation: string;

    constructor(regionName: string, azureLocation: string) {
        this.name = regionName;
        this.azureLocation = azureLocation;
    }

}

export class Plane {
    readonly name: string;
    readonly environment: Environment;
    readonly instances: Instance[] = [];
    outputNames: PlaneNames;
    subscriptionName: string;
    subscriptionId: string;

    constructor(name: string, environment: Environment) {
        this.name = name;
        this.environment = environment;
    }

    addInstance(instance: Instance): void {
        this.instances.push(instance);
    }

    generateNamesJson(environmentNames: EnvironmentNames): PlaneNames {
        this.outputNames = ResourceNames.sortKeys(new PlaneNames(environmentNames, this.name, this.subscriptionName, this.subscriptionId));
        return this.outputNames;
    }

    clone(): Plane {
        return cloneDeep<Plane>(this);
    }
}

export class Environment {
    readonly name: string;
    readonly pme: boolean;
    readonly primaryLocation: DataLocation;
    readonly planes: Plane[] = [new Plane("ops", this), new Plane("ctl", this), new Plane("data", this)];
    outputNames: EnvironmentNames;

    constructor(name: string, pme: boolean, primaryLocation: DataLocation) {
        this.name = name;
        this.pme = pme;
        this.primaryLocation = primaryLocation;
    }

    get allStampAzureLocations(): string[] {
        const allInstances = this.planes.flatMap(p => p.instances);
        const allStamps = allInstances.flatMap(i => i.stamps);
        const allStampAzureLocations = Array.from(new Set(allStamps.map(s => s.location.azureLocation)));
        return allStampAzureLocations;
    }

    generateNamesJson(componentNames: ComponentNames): EnvironmentNames {
        this.outputNames = ResourceNames.sortKeys(new EnvironmentNames(componentNames, this.name, this.primaryLocation.azureLocation, this.allStampAzureLocations));
        return this.outputNames;
    }

    clone(): Environment {
        return cloneDeep<Environment>(this);
    }
}

export class Instance {
    readonly name: string;
    readonly plane: Plane;
    readonly stamps: Stamp[] = [];
    readonly primaryLocation: DataLocation;
    outputNames: InstanceNames;

    constructor(name: string, plane: Plane) {
        this.name = name;
        this.plane = plane;
        this.primaryLocation = this.plane.environment.primaryLocation;
    }

    get instanceLocations(): DataLocation[] {
        return this.stamps.flatMap((n) => n.location);
    }

    get instanceRegions(): string[] {
        return this.instanceLocations.map(n => `${n.geography.name}-${n.region.name}`);
    }

    get instanceAzureLocations(): string[] {
        return this.instanceLocations.map(n => n.region.azureLocation);
    }

    addStamp(stamp: Stamp): void {
        this.stamps.push(stamp);
    }

    generateNamesJson(planeNames: PlaneNames): InstanceNames {
        this.outputNames = ResourceNames.sortKeys(new InstanceNames(
            planeNames,
            this.name,
            this.primaryLocation.azureLocation,
            this.instanceRegions,
            this.instanceAzureLocations));
        return this.outputNames;
    }

    clone(): Instance {
        return cloneDeep<Instance>(this);
    }
}

export class Stamp {
    readonly name: string;
    readonly location: DataLocation;

    constructor(name: string, location: DataLocation) {
        this.name = name;
        this.location = location;
    }

    get dataLocation(): DataLocation {
        // The stamp data plane location must be the same as the stamp control plane location
        return this.location;
    }

    clone(): Stamp {
        return cloneDeep<Stamp>(this);
    }
}

export class DataLocation {
    geography: Geography;
    region: Region;
    outputNames: RegionNames;

    get azureLocation(): string {
        return this.region.azureLocation;
    }

    generateNamesJson(instanceNames: InstanceNames): RegionNames {
        const geo = this.geography.name;
        const regionSuffix = this.region.name;
        const regionName = `${geo}-${regionSuffix}`;
        const regionLocation = this.region.azureLocation;
        this.outputNames = ResourceNames.sortKeys(new RegionNames(
            instanceNames, regionName, geo, regionSuffix, regionLocation));
        return this.outputNames;
    }

    clone(): DataLocation {
        return cloneDeep<DataLocation>(this);
    }
}
