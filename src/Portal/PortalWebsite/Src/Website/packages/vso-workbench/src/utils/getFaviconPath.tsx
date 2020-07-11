import { IPartnerInfo, getFeatureSet } from 'vso-client-core';
import { VSCodespacesPlatformInfo } from 'vs-codespaces-authorization';
import { getVSCodeAssetPath } from './getVSCodeAssetPath';

/**
 * Function to infer the favicon path based on `vscode channel`(stable/insider) and
 * the optional `favicon` setting in the platform info.
 */
export const getFaviconPath = (platformInfo: IPartnerInfo | VSCodespacesPlatformInfo | null): string => {
    if (!platformInfo || !('favicon' in platformInfo)) {
        return getVSCodeAssetPath('favicon.ico');
    }

    const { favicon } = platformInfo;
    const featureSet = getFeatureSet();

    if (!favicon) {
        return getVSCodeAssetPath('favicon.ico');
    }

    return favicon[featureSet];
};
