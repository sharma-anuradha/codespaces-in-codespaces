import { VSCodeQuality } from './vscode';
import { IPackageJson } from '../interfaces/IPackageJson';

const packageJson: IPackageJson = require('../../package.json');

export const customContainers = 'customContainers';
export const azureDevOpsOAuth = 'azureDevOpsOAuth';

const stableFeatures: string[] = [customContainers];
const insiderFeatures: string[] = [azureDevOpsOAuth];

export interface VSCodeConfig {
    commit: string;
    quality: VSCodeQuality;
}

export function evaluateFeatureFlag(flag: string): boolean {
    let featureSet = getVscodeQuality();

    if (featureSet === 'insider') {
        return insiderFeatures.includes(flag) || stableFeatures.includes(flag);
    }

    return stableFeatures.includes(flag);
}

export function getVscodeQuality(): VSCodeQuality {
    return window.localStorage.getItem('vso-featureset') === 'insider' ? 'insider' : 'stable';
}

export function getVSCodeVersion(quality: VSCodeQuality = getVscodeQuality()): VSCodeConfig {
    return {
        commit: packageJson.vscodeCommit[quality],
        quality,
    };
}

export function getVSCodeAssetPath(
    relativePath: string,
    quality: VSCodeQuality = getVscodeQuality()
) {
    const pathParts = [
        '/static/web-standalone',
        getVSCodeVersion(quality).commit.substr(0, 7),
        relativePath,
    ];

    return pathParts.join('/').replace(/\/+/g, '/');
}
