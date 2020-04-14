import { getFeatureSet, IPackageJson, IVSCodeConfig } from 'vso-client-core';

export const packageJSON: IPackageJson = require('../../package.json');

if (!packageJSON) {
    throw new Error('No package.json found.');
}

export const getVSCodeVersion = (): IVSCodeConfig => {
    const quality = getFeatureSet();

    if (!packageJSON.vscodeCommit) {
        throw new Error('No VSCode commit info found in the package.json');
    }

    const commit = packageJSON.vscodeCommit[quality];

    if (!commit) {
        throw new Error('No VSCode commit found in the package.json');
    }

    return { quality, commit };
};
