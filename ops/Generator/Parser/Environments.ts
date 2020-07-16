import { find } from "lodash";

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
        env.instances.push(inst);
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

export class Environment {
  name: string;
  pme: boolean;
  instances: Instance[] = [];
  outputNames: EnvironmentName;

  generateNamesJson(globalPrefix: string, compPrefix: string): EnvironmentName {
    return this.outputNames = {
        baseEnvName: `${globalPrefix}-${compPrefix}-${this.name}`,
        baseName: compPrefix,
        component: compPrefix,
        env: this.name,
        id: `${globalPrefix}-${compPrefix}-${this.name}`,
        nameFile: `${compPrefix}.${this.name}.names.json`,
        outputPath: `${compPrefix}.${this.name}`
      }
    }
}

export class EnvironmentName {
  baseEnvName: string;
  baseName: string;
  component: string;
  env: string;
  id: string;
  nameFile: string;
  outputPath: string;
}

export class Instance {
  name: string;
  stamps: Stamp[] = [];
  outputNames: InstanceName;

  generateNamesJson(globalPrefix: string, env: string, plane: string, regions: DataLocation[], compPrefix: string): InstanceName {
      return this.outputNames = {
        baseEnvName: `${globalPrefix}-${compPrefix}-${env}`,
        baseInstanceName: `${globalPrefix}-${compPrefix}-${env}-${plane}-${this.name}`,
        baseName: `${compPrefix}`,
        basePlaneName: `${globalPrefix}-${compPrefix}-${env}-${plane}`,
        component:  `${compPrefix}`,
        env:  `${env}`,
        id: `${globalPrefix}-${compPrefix}-${env}-${plane}-${this.name}`,
        instance: `${this.name}`,
        nameFile: `${compPrefix}.${env}-${plane}-${this.name}.names.json`,
        outputPath: `${compPrefix}.${env}-${plane}.${this.name}`,
        plane: `${plane}`,
        region: regions.map(n => `${n.geography.name}-${n.region.name}`)
      }
  }
}

export class InstanceName {
  baseEnvName: string;
  baseInstanceName: string;
  baseName: string;
  basePlaneName: string;
  component: string;
  env: string;
  id: string;
  instance: string;
  nameFile: string;
  outputPath: string;
  plane: string;
  region: string[];
}

export class Stamp {
  name: string;
  location: DataLocation;
  dataLocations: DataLocation[] = [];
}

export class DataLocation {
    geography: Geography;
    region: Region;
    env: string;
    outputNames: DataLocationName;

    generateNamesJson(globalPrefix: string, env: string, plane: string, instance: string, compPrefix: string): DataLocationName {
        return this.outputNames = {
            baseEnvName: `${globalPrefix}-${compPrefix}-${env}`,
            baseInstanceName: `${globalPrefix}-${compPrefix}-${env}-${plane}-${instance}`,
            baseName: `${compPrefix}`,
            basePlaneName: `${globalPrefix}-${compPrefix}-${env}-${plane}-${this.geography.name}-${this.region.name}`,
            baseRegionName: `${globalPrefix}-${compPrefix}-${env}-${plane}`,
            component:  `${compPrefix}`,
            env:  `${env}`,
            id: `${globalPrefix}-${compPrefix}-${env}-${plane}-${instance}`,
            instance: `${instance}`,
            nameFile: `${compPrefix}.${env}-${plane}-${instance}.names.json`,
            outputPath: `${compPrefix}.${env}-${plane}-${instance}`,
            plane: `${plane}`,
            region: `${this.geography.name}-${this.region.name}`
          }
    }
}

export class DataLocationName {
  baseEnvName: string;
  baseInstanceName: string;
  baseName: string;
  basePlaneName: string;
  baseRegionName: string;
  component: string;
  env: string;
  id: string;
  instance: string;
  nameFile: string;
  outputPath: string;
  plane: string;
  region: string;
}