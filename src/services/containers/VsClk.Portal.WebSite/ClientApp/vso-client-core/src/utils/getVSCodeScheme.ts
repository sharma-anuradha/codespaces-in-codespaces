import { getFeatureSet } from './getFeatureSet';
import { FeatureSet } from '../interfaces/FeatureSet';

export const getVSCodeScheme = () => {
    const quality = getFeatureSet();

    return quality === FeatureSet.Insider ? 'vscode-insiders' : 'vscode';
}
