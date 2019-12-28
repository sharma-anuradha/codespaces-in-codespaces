import { VSCodeQuality } from './vscode';

export const getVscodeQuality = (): VSCodeQuality => {
    return window.localStorage.getItem('vso-featureset') === 'insider' ? 'insider' : 'stable';
};
