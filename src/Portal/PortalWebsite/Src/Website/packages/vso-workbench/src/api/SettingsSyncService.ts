import { arrayUnique } from 'vso-client-core';
import { VSCodeDefaultAuthSession } from 'vs-codespaces-authorization';

import { TSettingsSyncResourceId } from '../interfaces/TSettingsSyncResourceId';
import { ISettingsSyncServiceResponse } from '../interfaces/ISettingsSyncServiceResponse';
import { ISettingsSyncVSCodeExtension } from '../interfaces/ISettingsSyncVSCodeExtension';
import { authService } from '../auth/authService';

export const SETTINGS_THEME_KEY = 'workbench.colorTheme';
export const GITHUB_THEME_NAME = 'GitHub Light';

export const getGithubDefaultSettings = () => {
    return {
        [SETTINGS_THEME_KEY]: GITHUB_THEME_NAME,
    };
};

export const isLightThemeSetting = (settings: Record<string, any>): boolean => {
    const theme = settings[SETTINGS_THEME_KEY];
    if (!theme) {
        // we have the light theme by default in the web bits
        return true;
    }
    if (theme === GITHUB_THEME_NAME) {
        return true;
    }
    return false;
};

export const isThemeExtension = (extensionId: string): boolean => {
    return extensionId.indexOf('theme') !== -1;
};

export const getSettingsSyncExtensions = async (defaultExtensions: string[]): Promise<string[]> => {
    const session = await authService.getSettingsSyncSession();
    if (!session) {
        return [];
    }

    const settingsSyncService = new SettingsSyncService(session);

    return await settingsSyncService.getExtensions(defaultExtensions);
};

export const getSettingsSyncSettings= async (): Promise<Record<string, any>> => {
    const session = await authService.getSettingsSyncSession();
    if (!session) {
        return {};
    }

    const settingsSyncService = new SettingsSyncService(session);

    return await settingsSyncService.getSettings();
};

export class SettingsSyncService {
    constructor(private readonly credentials: VSCodeDefaultAuthSession) {}

    private makeRequest = async (resourceId: TSettingsSyncResourceId): Promise<string | null> => {
        try {
            const { type, accessToken } = this.credentials;

            const result = await fetch(`/settings-sync?resourceId=${resourceId}`, {
                headers: {
                    'x-account-type': type,
                    'Authorization': `Bearer ${accessToken}`,
                },
            });

            if (!result.ok) {
                return null;
            }

            const json: ISettingsSyncServiceResponse = await result.json();
            return json.content;
        } catch (e) {
            return null;
        }
    };

    public getSettings = async (defaultSettings = {}): Promise<Record<string, any>> => {
        try {
            const settingsString = await this.makeRequest('settings');
            if (!settingsString) {
                return {
                    ...defaultSettings,
                };
            }

            const settings = JSON.parse(JSON.parse(settingsString).settings);

            return {
                ...defaultSettings,
                ...settings,
            };
        } catch (e) {
            return {
                ...defaultSettings,
            };
        }
    };

    public getExtensions = async (defaultExtenstions: string[] = []): Promise<string[]> => {
        try {
            /**
             * The Settings Sync Service also returns the built in extensions that must be filtered out
             * since trying to install those on VSCode Server startup will result in user facing error.
             */
            const allowedVSCodeExtensions = ['ms-vscode.azure-account'];

            const extensionsString = await this.makeRequest('extensions');
            if (!extensionsString) {
                return [...defaultExtenstions];
            }

            const extensions: ISettingsSyncVSCodeExtension[] = JSON.parse(extensionsString);
            const extensionStrings = extensions
                .filter((ex: ISettingsSyncVSCodeExtension) => {
                    const { identifier } = ex;
                    const { id, uuid } = identifier;

                    const isUuid = !!uuid;
                    const isVSCode = id.startsWith('ms-vscode') || id.startsWith('vscode');
                    const isAllowedVSCodeExtension = allowedVSCodeExtensions.includes(id);

                    return isUuid && (!isVSCode || isAllowedVSCodeExtension);
                })
                .map((ex) => {
                    return ex.identifier.id;
                });

            const result = arrayUnique([...defaultExtenstions, ...extensionStrings]);

            return result;
        } catch (e) {
            return [...defaultExtenstions];
        }
    };
}
