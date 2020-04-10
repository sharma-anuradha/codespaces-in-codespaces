import { vsls } from 'vso-client-core';

import {
    BrowserSyncService as BrowserSyncServiceBase,
    BrowserConnectorMessages,
} from 'vso-ts-agent';

import { authService } from '../services/authService';

import { loginPath, environmentsPath } from '../routerPaths';

export class BrowserSyncService extends BrowserSyncServiceBase {
    public async onSourceEvent(e: vsls.SourceEventArgs) {
        switch (e.sourceId) {
            case BrowserConnectorMessages.ConnectToEnvironment: {
                const payload = JSON.parse(e.jsonContent);
                const { id } = payload;
                location.href = `/environment/${id}`;
                return;
            }
            case BrowserConnectorMessages.DisconnectFromEnvironment: {
                location.href = environmentsPath;
                return;
            }
            case BrowserConnectorMessages.SignOut: {
                await authService.logout();
                location.href = loginPath;
                return;
            }
            case BrowserConnectorMessages.CopyServerUrl:
            case BrowserConnectorMessages.OpenPortInBrowser:
            case BrowserConnectorMessages.ForwardPort:
                return;

            case BrowserConnectorMessages.GetLocalStorageValueRequest:
                const payload = JSON.parse(e.jsonContent);
                const { key } = payload;

                const valueJson = localStorage.getItem(key);

                let value: any = undefined;
                if (valueJson) {
                    try {
                        value = JSON.parse(valueJson);
                    } catch {
                        // do nothing, just return undefined
                    }
                }

                const repsonse = JSON.stringify({
                    key,
                    value,
                });

                await this.sourceEventService.fireEventAsync(
                    BrowserConnectorMessages.GetLocalStorageValueResponse,
                    repsonse
                );
                return;
        }
    };
}