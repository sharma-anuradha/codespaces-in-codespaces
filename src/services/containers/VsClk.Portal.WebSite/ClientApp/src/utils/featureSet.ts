
export const customContainers = 'customContainers';

const stableFeatures: string[] = [customContainers];

const insiderFeatures: string[] = [];

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