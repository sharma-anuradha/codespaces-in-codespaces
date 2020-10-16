// ContainerRegistryValues.ts

import { IPlaneNames } from "./ResourceNameDefs";

export interface IContainerRegistryValues {
    readonly containerRegistryName: string;
    readonly imageRepositoryUrl: string;
    readonly containerRegistryReplications: any[];
}

export abstract class ContainerRegistryValues {

    public static assignValues(plane: IPlaneNames): IContainerRegistryValues {

        if (plane.plane === "ctl") {
            const containerRegistryName = plane.basePlaneName.replace(/-/g, '') + 'acr';
            const replications = plane.environmentStampLocations.map(location => {
                location = location.toLowerCase();
                return {
                    name: `${containerRegistryName}/${location}`,
                    condition: `[not(equals(toLower(variables('location')),'${location}'))]`,
                    type: "Microsoft.ContainerRegistry/registries/replications",
                    apiVersion: "2019-12-01-preview",
                    location: location,
                    properties: {
                        regionEndpointEnabled: true
                    },
                    dependsOn: [
                        `[resourceId('Microsoft.ContainerRegistry/registries','${containerRegistryName}')]`
                    ]
                };
            });


            const values: IContainerRegistryValues  = {
                containerRegistryName: containerRegistryName,
                imageRepositoryUrl: `${containerRegistryName}.azurecr.io`,
                containerRegistryReplications: replications
            }

            Object.assign(plane, values);
            return values;
        }

        return null;
    }

}
