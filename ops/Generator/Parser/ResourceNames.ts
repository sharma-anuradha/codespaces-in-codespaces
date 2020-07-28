
export class ComponentNames {
    baseName: string;
    prefix: string;
    component: string;
}

export class EnvironmentNames extends ComponentNames {
    baseEnvironmentName: string;
    env: string;
}

export class PlaneNames extends EnvironmentNames {
    basePlaneName: string;
    plane: string;
}

export class InstanceNames extends PlaneNames {
    baseInstanceName: string;
    instance: string;
    instanceRegions: string[];
}

export class RegionNames extends InstanceNames {
    baseRegionName: string;
    geo: string;
    region: string;
    location: string;
}

export default abstract class ResourceNames {

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
}