import { find, cloneDeep } from "lodash";
import ResourceNames, { EnvironmentNames, InstanceNames, RegionNames, PlaneNames, ComponentNames } from "./ResourceNames";

export class EnvironmentsDeployment implements IEnvironmentsDeployment {
    globalPrefix: string;
    environments: Environment[] = [];
    geographies: Geography[] = [];

    constructor(environments: any) {
        this.parseEnvironmentsDeploymentsJson(environments);
    }

    private getGeography(name: string): Geography {
        return find(this.geographies, ["name", name]);
    }

    private getRegion(geo: Geography, name: string): Region {
        return find(geo.regions, ["name", name]);
    }

    private parseEnvironmentsDeploymentsJson(env: any) {
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

    private parseEnvironmentsJson(environments: any) {
        for (const envName in environments) {
            const env = new Environment();
            env.name = envName;
            const envObj = environments[envName];
            env.pme = envObj.pme;
            const envLocation = this.getDataLocation(envObj.location).region.fullName;
            for (const plane of env.planes) {
                for (const instanceName in envObj.instances) {
                    const instance = envObj.instances[instanceName];
                    const inst = new Instance();
                    inst.name = instanceName;
                    inst.location = envLocation;
                    for (const stampName in instance.stamps) {
                        const smp = new Stamp();
                        smp.name = stampName;
                        const stamp = instance.stamps[stampName];
                        smp.location = this.getDataLocation(stamp.location);
                        for (const data of stamp.dataLocations) {
                            smp.dataLocations.push(this.getDataLocation(data));
                        }
                        inst.stamps.push(smp);
                    }
                    plane.instances.push(inst);
                }
            }
            this.environments.push(env);
        }
    }

    private parseGeographiesJson(geos: any) {
        for (const geoName in geos) {
            const geo = new Geography();
            geo.name = geoName;
            for (const regName in geos[geoName]) {
                const reg = new Region();
                reg.name = regName;
                reg.fullName = geos[geoName][regName];
                geo.regions.push(reg);
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
    name: string;
    regions: Region[] = [];
}

export class Region {
    name: string;
    fullName: string;
}

export class Plane {
    name: string;
    instances: Instance[] = [];
    outputNames: PlaneNames;
    subscriptionName: string;
    subscriptionId: string;

    constructor(name: string) {
        this.name = name;
    }

    generateNamesJson(environmentNames: EnvironmentNames): PlaneNames {
        this.outputNames = ResourceNames.sortKeys(new PlaneNames(environmentNames, this.name, this.subscriptionName, this.subscriptionId));
        return this.outputNames;
    }

    clone() : Plane {
        return cloneDeep<Plane>(this);
    }
}

export class Environment {
    name: string;
    pme: boolean;
    planes: Plane[] = [new Plane("ops"), new Plane("ctl"), new Plane("data")];
    outputNames: EnvironmentNames;

    generateNamesJson(componentNames: ComponentNames): EnvironmentNames {
        this.outputNames = ResourceNames.sortKeys(new EnvironmentNames(componentNames, this.name));
        return this.outputNames;
    }

    clone() : Environment {
        return cloneDeep<Environment>(this);
    }
}

export class Instance {
    name: string;
    stamps: Stamp[] = [];
    location: string;
    outputNames: InstanceNames;

    generateNamesJson(planeNames: PlaneNames, regions: DataLocation[]): InstanceNames {
        const instanceRegions = regions.map(n => `${n.geography.name}-${n.region.name}`);
        this.outputNames = ResourceNames.sortKeys(new InstanceNames(planeNames, this.name, this.location, instanceRegions));
        return this.outputNames;
    }

    clone(): Instance {
        return cloneDeep<Instance>(this);
    }
}

export class Stamp {
    name: string;
    location: DataLocation;
    dataLocations: DataLocation[] = [];

    clone(): Stamp {
        return cloneDeep<Stamp>(this);
    }
}

export class DataLocation {
    geography: Geography;
    region: Region;
    outputNames: RegionNames;

    generateNamesJson(instanceNames: InstanceNames): RegionNames {
        const geo = this.geography.name;
        const regionSuffix = this.region.name;
        const regionName = `${geo}-${regionSuffix}`;
        const regionLocation = this.region.fullName;
        this.outputNames = ResourceNames.sortKeys(new RegionNames(instanceNames, regionName, geo, regionSuffix, regionLocation));
        return this.outputNames;
    }

    clone(): DataLocation {
        return cloneDeep<DataLocation>(this);
    }
}
