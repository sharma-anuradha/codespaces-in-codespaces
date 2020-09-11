import { getVSCodeScheme } from 'vso-client-core';

import { authService } from '../../../../auth/authService';
import { IAuthStrategy } from '../../../../interfaces/IAuthStrategy';

/**
 * For performance reasons, VSCode Settings Sync Service reads the default auth session
 * that meant to be used for the settings sync using the `${getVSCodeScheme()}.login` key
 * directly from `keytar`. Otherwise the service will need to wait on the VSCode native
 * auth providers to initialize (which are built in vscode extensions).
 * This record also used to provide the information to the vscode workbench on what session
 * from the `vscodeSettings.defaultAuthSessions` is used for settings sync so that can be
 * shown in the vscode "profile" UI. Hence if the `vscodeSettings.authenticationSessionId`
 * property is provided, we find the corresponding session record in `vscodeSettings.defaultAuthSessions`
 * list and fulfill the below request with it.
 */
export class SettingsSyncStrategy implements IAuthStrategy {
    public async canHandleService(service: string, account: string) {
        if (account !== 'account') {
            return false;
        }

        if (service !== `${getVSCodeScheme()}.login`) {
            return false;
        }

        return !!(await authService.getSettingsSyncSession());
    }

    public async getToken(service: string, account: string): Promise<string | null> {
        const ghToken = await authService.getCachedGithubToken();
        const settingsSyncSession = await authService.getSettingsSyncSession();
        if (!ghToken || !settingsSyncSession) {
            return null;
        }

        const { id, type: providerId, accessToken } = settingsSyncSession;
        // note that this record does not have the usual VSCodeAuthSession
        // shape and no scopes but the token expected to have at least
        // the `email` scope so that the settings sync service can get that
        // info for GitHub public API endpoints
        return JSON.stringify({
            id,
            providerId,
            accessToken,
        });
    }
}
