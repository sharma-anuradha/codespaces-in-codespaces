import { FeatureSet } from '../interfaces/FeatureSet';

export const getFeatureSet = (): FeatureSet => {
    const vsoFeatureSet = window.localStorage.getItem('vso-featureset') || FeatureSet.Stable;

    return vsoFeatureSet === FeatureSet.Insider ? FeatureSet.Insider : FeatureSet.Stable;
};
