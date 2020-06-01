import { UserDataProvider } from '../providers/userDataProvider/userDataProvider';
import { getDefaultSettings } from './getDefaultSettings';
import { SettingsSyncService, getSettingsSyncSettings } from '../../api/SettingsSyncService';

export const getUserDataProvider = async () => {
    const userDataProvider = new UserDataProvider(async () => {
        /**
         * TODO: merge default settings
         */
        const [defaultSettings, syncedSettings] = await Promise.all([
            getDefaultSettings(),
            getSettingsSyncSettings(),
        ]);

        if (syncedSettings && Object.keys(syncedSettings).length) {
            return JSON.stringify(syncedSettings);
        }

        return defaultSettings || '';
    });

    await userDataProvider.initializeDBProvider();
    return userDataProvider;
};
