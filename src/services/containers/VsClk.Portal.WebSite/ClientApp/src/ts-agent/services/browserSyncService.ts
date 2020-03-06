import { SourceEventService, SourceEventArgs } from '../contracts/VSLS';
import { WorkspaceClient } from '../workspaceClient';
import { authService } from '../../services/authService';
import { setAuthCookie } from '../../utils/setAuthCookie';
import { loginPath, environmentsPath } from '../../routerPaths';
import { getAuthTokenAction } from '../../actions/getAuthTokenActionCommon';

export enum BrowserConnectorMessages {
    ConnectToEnvironment = 'VSO_BrowserSync_ConnectToEnvironment',
    DisconnectFromEnvironment = 'VSO_BrowserSync_DisconnectFromEnvironment',
    OpenPortInBrowser = 'VSO_BrowserSync_OpenPortInBrowser',
    CopyServerUrl = 'VSO_BrowserSync_CopyServerUrl',
    ForwardPort = 'VSO_BrowserSync_ForwardPort',
    SignOut = 'VSO_BrowserSync_SignOut',
    GetLocalStorageValueRequest = 'VSO_BrowserSync_GetLocalStorageValue_Request',
    GetLocalStorageValueResponse = 'VSO_BrowserSync_GetLocalStorageValue_Response',
}

export class BrowserSyncService {
    private constructor(private readonly sourceEventService: SourceEventService) {
        this.sourceEventService.onEvent(this.onSourceEvent);
    }

    public static init(workspaceClient: WorkspaceClient): void {
        const sourceEventService = workspaceClient.getServiceProxy<SourceEventService>(
            SourceEventService
        );

        new BrowserSyncService(sourceEventService);
    }

    private redirect(url: string) {
        // TODO: @Oleg trigger soft SPA redirection
        location.href = url;
    }

    private onSourceEvent = async (e: SourceEventArgs) => {
        switch (e.sourceId) {
            case BrowserConnectorMessages.ConnectToEnvironment: {
                const payload = JSON.parse(e.jsonContent);
                const { id } = payload;
                this.redirect(`/environment/${id}`);
                return;
            }
            case BrowserConnectorMessages.DisconnectFromEnvironment: {
                this.redirect(environmentsPath);
                return;
            }
            case BrowserConnectorMessages.SignOut: {
                await authService.logout();
                this.redirect(loginPath);
                return;
            }
            case BrowserConnectorMessages.CopyServerUrl:
            case BrowserConnectorMessages.OpenPortInBrowser:
            case BrowserConnectorMessages.ForwardPort:
                const getAuthToken = getAuthTokenAction();
                const token = await getAuthToken();

                if (token === undefined) {
                    return;
                }
                await setAuthCookie(token);
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
