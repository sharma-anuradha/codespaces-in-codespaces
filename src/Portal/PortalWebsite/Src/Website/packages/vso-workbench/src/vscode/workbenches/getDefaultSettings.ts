import { getCurrentEnvironmentId, authService } from 'vso-client-core';

const CODESPACES_DEFAULT_SETTINGS = {
    'workbench.startupEditor': 'readme',
}

export const getDefaultSettings = async (): Promise<Record<string, any>> => {
    const info = await authService.getCachedPartnerInfo(getCurrentEnvironmentId());
    if (!info) {
        return {};
    }

    // old payload does not have the settings property
    const defaultSettings = ('vscodeSettings' in info)
        ? info.vscodeSettings.defaultSettings
        : undefined;

    return {
        ...CODESPACES_DEFAULT_SETTINGS,
        ...defaultSettings,
    }
};
