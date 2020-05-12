import { FeatureSet } from '../interfaces/FeatureSet';

export const getFeatureSet = (): FeatureSet => {
    const params = new URLSearchParams(location.search);
    const paramsFeatureSet = params.get('dogfoodChannel');

    const vsoFeatureSet = window.localStorage.getItem('vso-featureset');

    let vscodeQuality = FeatureSet.Stable;
    if (paramsFeatureSet === FeatureSet.Insider) {
        vscodeQuality = FeatureSet.Insider;
    }
    
    if (vsoFeatureSet === FeatureSet.Insider) {
        vscodeQuality = FeatureSet.Insider;
    }

    if (vsoFeatureSet === FeatureSet.Stable) {
        vscodeQuality = FeatureSet.Stable;
    }

    return vscodeQuality;
};
