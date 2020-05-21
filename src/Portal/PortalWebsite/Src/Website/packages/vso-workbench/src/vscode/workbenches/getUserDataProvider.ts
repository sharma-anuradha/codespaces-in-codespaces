import { UserDataProvider } from '../providers/userDataProvider/userDataProvider';
import { getDefaultSettings } from './getDefaultSettings';
import { SettingsSyncService } from '../../api/SettingsSyncService';

const getDefaultSettingsSyncSettings = async () => {
    await SettingsSyncService.init();

    return await SettingsSyncService.Singleton.getSettings();
};

export const getUserDataProvider = async () => {
    const userDataProvider = new UserDataProvider(async () => {
        /**
         * TODO: merge default settings
         */
        const [defaultSettings, syncedSettings] = await Promise.all([
            getDefaultSettings(),
            getDefaultSettingsSyncSettings(),
        ]);

        if (syncedSettings && Object.keys(syncedSettings).length) {
            return JSON.stringify(syncedSettings);
        }

        return defaultSettings || '';
    });

    await userDataProvider.initializeDBProvider();
    return userDataProvider;
};
