/**
 * Historically, we were using the static list of extensions to preinstall,
 * after moving to the `platform` based approach, we expect the `partner-info` to
 * specify the default extensions in the payload.
 */

import {
    getCurrentEnvironmentId,
    authService,
    arrayUnique
} from 'vso-client-core';

import {
    PLATFORM_REQUIRED_EXTENSIONS
} from '../../constants';
import { getSettingsSyncExtensions } from '../../api/SettingsSyncService';

export const getDefaultSettingsExtensions = async (): Promise<string[]> => {
    const info = await authService.getCachedPartnerInfo(getCurrentEnvironmentId());
    if (!info) {
        return [];
    }

    if (!('vscodeSettings' in info)) {
        return [];
    }

    const { defaultExtensions } = info.vscodeSettings;

    if (defaultExtensions) {
        return defaultExtensions.map((extension) => {
            return extension.id;
        });
    }

    return [];
};

export const getExtensions = async (isFirstRun: boolean): Promise<string[]> => {
    const settingsDefaultExtensions = await getDefaultSettingsExtensions();

    if (!isFirstRun) {
        return [ ...PLATFORM_REQUIRED_EXTENSIONS ];
    }

    const settingsSyncExtensions = await getSettingsSyncExtensions(PLATFORM_REQUIRED_EXTENSIONS);

    return arrayUnique([
        ...PLATFORM_REQUIRED_EXTENSIONS,
        ...settingsDefaultExtensions,
        ...settingsSyncExtensions,
    ]);
};
