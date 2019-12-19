
export const customContainers = 'customContainers';

const stableFeatures: string[] = [];

const insiderFeatures = [customContainers];

export function getFeatureSet() : string
{
    return window.localStorage.getItem('vso-featureset') || 'stable';
}

export function evaluateFeatureFlag(flag: string) : boolean
{
    let featureSet = getFeatureSet();

    if (featureSet === 'insider') {
        return insiderFeatures.includes(flag);
    }

    return stableFeatures.includes(flag);
}