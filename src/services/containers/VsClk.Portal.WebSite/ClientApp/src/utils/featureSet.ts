
export const customContainers = 'customContainers';
export const azureDevOpsOAuth = 'azureDevOpsOAuth';

const stableFeatures: string[] = [customContainers];

const insiderFeatures: string[] = [azureDevOpsOAuth];

export function getFeatureSet() : string
{
    return window.localStorage.getItem('vso-featureset') || 'stable';
}

export function evaluateFeatureFlag(flag: string) : boolean
{
    let featureSet = getFeatureSet();

    if (featureSet === 'insider') {
        return insiderFeatures.includes(flag) || stableFeatures.includes(flag);
    }

    return stableFeatures.includes(flag);
}