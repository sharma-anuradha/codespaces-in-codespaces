import { getCurrentEnvironmentId, authService } from 'vso-client-core';
import { IHomeIndicator } from 'vscode-web';

export const getHomeIndicator = async (): Promise<IHomeIndicator | undefined> => {
    const info = await authService.getCachedPartnerInfo(getCurrentEnvironmentId());
    if (!info) {
        return;
    }
    // old payload does not have the settings property
    const homeIndicator = ('cascadeToken' in info && info.vscodeSettings)
        ? info.vscodeSettings.homeIndicator
        : undefined;
    return homeIndicator;
};
