import { UserDataProvider } from '../providers/userDataProvider/userDataProvider';
import { getDefaultSettings } from './getDefaultSettings';
import { getSettingsSyncSettings } from '../../api/SettingsSyncService';

export const getUserDataProvider = async () => {
    const userDataProvider = new UserDataProvider(async () => {
        const [defaultSettings, syncedSettings] = await Promise.all([
            getDefaultSettings(),
            getSettingsSyncSettings(),
        ]);

        if (syncedSettings && Object.keys(syncedSettings).length) {
            const mergedSettings = {
                ...defaultSettings,
                ...syncedSettings,
            };  
            return JSON.stringify(mergedSettings);
        }

        return JSON.stringify(defaultSettings);
    });

    await userDataProvider.initializeDBProvider();
    return userDataProvider;
};
