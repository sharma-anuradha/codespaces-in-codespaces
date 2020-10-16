// Cluster Values

import { IRegionNames } from "./ResourceNameDefs";

// Update this version to deploy a new sxs cluters
const clusterVersion = 1;
const clusterVersionSuffix = `v${clusterVersion}`;

// The cluster kubernetes version
const kubernetesVersion = "1.18.6";

// Ingress controller constants
const ingressReplicaCount = 3;
const ingressReplicaCountDev = 1;

export interface IClusterValues {
    readonly clusterName: string;
    readonly clusterVersionSuffix: string;
    readonly clusterKubernetesVersion: string;
    readonly clusterIngressReplicaCount: number;
}

export abstract class ClusterValues {

    public static assignValues(target: IRegionNames): IClusterValues {
        if (target.plane === "ctl") {
            const component = target.component;
            if (component === 'codesp') {
                const coreRegionResourceName = target.baseRegionName;
                const values: IClusterValues = {
                    clusterName: `${coreRegionResourceName}-cluster-${clusterVersionSuffix}`,
                    clusterVersionSuffix: clusterVersionSuffix,
                    clusterKubernetesVersion: kubernetesVersion,
                    clusterIngressReplicaCount: target.env === "dev" ? ingressReplicaCountDev : ingressReplicaCount
                };
                Object.assign(target, values);
                return values;
            }
        }

        return null;
    }

}

