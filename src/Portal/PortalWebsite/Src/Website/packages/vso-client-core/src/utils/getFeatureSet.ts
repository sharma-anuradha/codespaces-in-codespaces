import { FeatureSet } from '../interfaces/FeatureSet';
import { VSCS_FEATURESET_LOCALSTORAGE_KEY } from '../constants';

export const getFeatureSet = (): FeatureSet => {
    const params = new URLSearchParams(location.search);
    const paramsFeatureSet = params.get('dogfoodChannel');

    const vsoFeatureSet = window.localStorage.getItem(VSCS_FEATURESET_LOCALSTORAGE_KEY);

    let vscodeQuality = FeatureSet.Stable;
    if (paramsFeatureSet === FeatureSet.Insider) {
        vscodeQuality = FeatureSet.Insider;
        // the query param should take precendence over the localstorage record
        return vscodeQuality;
    }

    if (paramsFeatureSet === FeatureSet.Stable) {
        vscodeQuality = FeatureSet.Stable;
        // the query param should take precendence over the localstorage record
        return vscodeQuality;
    }
    
    if (vsoFeatureSet === FeatureSet.Insider) {
        vscodeQuality = FeatureSet.Insider;
    }

    return vscodeQuality;
};
