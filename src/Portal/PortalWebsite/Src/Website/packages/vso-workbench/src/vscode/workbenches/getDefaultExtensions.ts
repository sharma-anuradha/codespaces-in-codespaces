/**
 * Historically, we were using the static list of extensions to preinstall,
 * after moving to the `platform` based approach, we expect the `codespace info` to
 * specify the default extensions in the payload.
 */

import { getCurrentEnvironmentId, authService, arrayUnique } from 'vso-client-core';

import { PLATFORM_REQUIRED_EXTENSIONS } from '../../constants';
import { getSettingsSyncExtensions } from '../../api/SettingsSyncService';

const getDefaultSettingsExtensions = async (): Promise<string[]> => {
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

const disabledRequiredExtensions: string[] = [];

export const addRequiredExtensionExclusion = (extensionId: string) => {
    if (!disabledRequiredExtensions.includes(extensionId)) {
        disabledRequiredExtensions.push(extensionId);
    }
};

export const removeRequiredExtensionExclusion = (extensionId: string) => {
    const index = disabledRequiredExtensions.indexOf(extensionId);
    if (index === -1) {
        return;
    }

    disabledRequiredExtensions.splice(index, 1);
};

const getFilteredDefaultExtensions = () => {
    return PLATFORM_REQUIRED_EXTENSIONS.filter((extension) => {
        return !disabledRequiredExtensions.includes(extension);
    });
};

export const getExtensions = async (isFirstRun: boolean): Promise<string[]> => {
    const settingsDefaultExtensions = await getDefaultSettingsExtensions();
    const requiredExtensions = getFilteredDefaultExtensions();

    if (!isFirstRun) {
        return [...requiredExtensions];
    }

    const settingsSyncExtensions = await getSettingsSyncExtensions(requiredExtensions);
    const result = arrayUnique([
        ...requiredExtensions,
        ...settingsDefaultExtensions,
        ...settingsSyncExtensions,
    ]);

    return result
};
