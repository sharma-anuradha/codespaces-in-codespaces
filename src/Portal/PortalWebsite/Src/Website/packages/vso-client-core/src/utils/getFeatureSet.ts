import { FeatureSet } from '../interfaces/FeatureSet';
import { VSCS_FEATURESET_LOCALSTORAGE_KEY } from '../constants';

let memoryFeatureSet: FeatureSet | null;

export const setFeatureSet = (valueToSet: FeatureSet | null) => {
    memoryFeatureSet = valueToSet;
}

export const getFeatureSet = (): FeatureSet => {
    const params = new URLSearchParams(location.search);
    const paramsFeatureSet = params.get('dogfoodChannel');

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

    const vsoFeatureSet = window.localStorage.getItem(VSCS_FEATURESET_LOCALSTORAGE_KEY);
    if (vsoFeatureSet === FeatureSet.Insider) {
        vscodeQuality = FeatureSet.Insider;
    }

    if (memoryFeatureSet) {
        return memoryFeatureSet;
    }

    return vscodeQuality;
};
