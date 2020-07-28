import { find } from "lodash";
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
            for (const plane of env.planes) {
                for (const instanceName in envObj.instances) {
                    const instance = envObj.instances[instanceName];
                    const inst = new Instance();
                    inst.name = instanceName;
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

    constructor(name: string) {
        this.name = name;
    }

    generateNamesJson(environmentNames: EnvironmentNames): PlaneNames {
        const baseName = ResourceNames.makeResourceName(environmentNames.prefix, environmentNames.component, environmentNames.env, this.name);

        return this.outputNames = {
            baseName: baseName,
            baseEnvironmentName: environmentNames.baseEnvironmentName,
            basePlaneName: baseName,
            prefix: environmentNames.prefix,
            component: environmentNames.component,
            env: environmentNames.env,
            plane: this.name
        }
    }

    clone() : Plane {
        const obj = new Plane(this.name);
        obj.instances = this.instances.map(i => i.clone());
        return obj;
    }
}

export class Environment {
    name: string;
    pme: boolean;
    planes: Plane[] = [new Plane("ops"), new Plane("ctl"), new Plane("data")];
    outputNames: EnvironmentNames;

    generateNamesJson(componentNames: ComponentNames): EnvironmentNames {
        const baseName = ResourceNames.makeResourceName(componentNames.prefix, componentNames.component, this.name);

        return this.outputNames = {
            baseName: baseName,
            baseEnvironmentName: baseName,
            prefix: componentNames.prefix,
            component: componentNames.component,
            env: this.name,
        }
    }

    clone() : Environment {
        const obj = new Environment();
        obj.name = this.name;
        obj.pme = this.pme;
        obj.planes = this.planes.map(p => p.clone());
        return obj;
    }
}

export class Instance {
    name: string;
    stamps: Stamp[] = [];
    outputNames: InstanceNames;

    generateNamesJson(planeNames: PlaneNames, regions: DataLocation[]): InstanceNames {
        const baseName = ResourceNames.makeResourceName(planeNames.prefix, planeNames.component, planeNames.env, planeNames.plane, this.name);
        return this.outputNames = {
            baseName: baseName,
            baseEnvironmentName: planeNames.baseEnvironmentName,
            basePlaneName: planeNames.basePlaneName,
            baseInstanceName: baseName,
            prefix: planeNames.prefix,
            component: planeNames.component,
            env: planeNames.env,
            plane: planeNames.plane,
            instance: this.name,
            instanceRegions: regions.map(n => `${n.geography.name}-${n.region.name}`)
        }
    }

    clone(): Instance {
        const obj = new Instance();
        obj.name = this.name;
        obj.stamps = this.stamps.map(s => s.clone());
        return obj;
    }
}

export class Stamp {
    name: string;
    location: DataLocation;
    dataLocations: DataLocation[] = [];

    clone(): Stamp {
        const obj = new Stamp();
        obj.name = this.name;
        obj.location = this.location.clone();
        obj.dataLocations = this.dataLocations.map(dl => dl.clone());
        return obj;
    }
}

export class DataLocation {
    geography: Geography;
    region: Region;
    outputNames: RegionNames;

    generateNamesJson(instanceNames: InstanceNames): RegionNames {
        const baseName = ResourceNames.makeResourceName(instanceNames.prefix, instanceNames.component, instanceNames.env, instanceNames.plane, instanceNames.instance, this.geography.name, this.region.name);
        const baseStorageName = ResourceNames.convertToStorageResourceName(instanceNames.prefix, baseName);
        return this.outputNames = {
            baseName: baseName,
            baseEnvironmentName: instanceNames.baseEnvironmentName,
            basePlaneName: instanceNames.basePlaneName,
            baseInstanceName: instanceNames.baseInstanceName,
            baseRegionName: baseName,
            baseRegionStorageName: baseStorageName,
            prefix: instanceNames.prefix,
            component: instanceNames.component,
            env: instanceNames.env,
            plane: instanceNames.plane,
            instance: instanceNames.instance,
            instanceRegions: instanceNames.instanceRegions,
            geo: this.geography.name,
            region: `${this.geography.name}-${this.region.name}`,
            location: this.region.fullName
        }
    }

    clone(): DataLocation {
        const obj = new DataLocation();
        obj.geography = this.geography;
        obj.region = this.region;
        return obj;
    }
}
