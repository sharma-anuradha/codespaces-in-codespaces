import { getCurrentEnvironmentId, authService } from 'vso-client-core';

export const getDefaultSettings = async (): Promise<string> => {
    const info = await authService.getCachedPartnerInfo(getCurrentEnvironmentId());
    if (!info) {
        return '';
    }

    // old payload does not have the settings property
    const defaultSettings = ('cascadeToken' in info && info.vscodeSettings)
        ? info.vscodeSettings.defaultSettings
        : undefined;

    return defaultSettings || '';
};
