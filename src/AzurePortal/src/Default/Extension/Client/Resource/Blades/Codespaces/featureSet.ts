export const customContainers = 'customContainers';
export const azureDevOpsOAuth = 'azureDevOpsOAuth';

const stableFeatures: string[] = [customContainers, azureDevOpsOAuth];
const insiderFeatures: string[] = [];

export function evaluateFeatureFlag(flag: string): boolean {
   /* let featureSet = getFeatureSet();

    if (featureSet === FeatureSet.Insider) {
        return insiderFeatures.includes(flag) || stableFeatures.includes(flag);
    }*/

    return stableFeatures.includes(flag);
}