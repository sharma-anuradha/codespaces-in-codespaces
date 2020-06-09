import { getCurrentEnvironmentId, authService } from 'vso-client-core';
import { VSCodeHomeIndicator } from 'vs-codespaces-authorization';

export const getHomeIndicator = async (): Promise<VSCodeHomeIndicator | undefined> => {
    const info = await authService.getCachedPartnerInfo(getCurrentEnvironmentId());
    if (!info) {
        return;
    }

    if (!('codespaceToken' in info)) {
        return;
    }

    if ('homeIndicator' in info.vscodeSettings) {
        return info.vscodeSettings.homeIndicator;
    }
};
