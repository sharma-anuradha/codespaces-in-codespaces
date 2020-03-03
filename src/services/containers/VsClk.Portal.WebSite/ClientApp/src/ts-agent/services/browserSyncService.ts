import { SourceEventService, SourceEventArgs } from '../contracts/VSLS';
import { WorkspaceClient } from '../workspaceClient';
import { authService } from '../../services/authService';
import { getAuthToken } from '../../actions/getAuthToken';
import { setAuthCookie } from '../../utils/setAuthCookie';
import { loginPath, environmentsPath } from '../../routerPaths';

export enum BrowserConnectorMessages {
    ConnectToEnvironment = 'VSO_BrowserSync_ConnectToEnvironment',
    DisconnectFromEnvironment = 'VSO_BrowserSync_DisconnectFromEnvironment',
    OpenPortInBrowser = 'VSO_BrowserSync_OpenPortInBrowser',
    CopyServerUrl = 'VSO_BrowserSync_CopyServerUrl',
    ForwardPort = 'VSO_BrowserSync_ForwardPort',
    SignOut = 'VSO_BrowserSync_SignOut',
}

export class BrowserSyncService {
    constructor(private readonly workspaceClient: WorkspaceClient) {}

    public async init(): Promise<void> {
        const sourceEventService = await this.workspaceClient.getServiceProxy<SourceEventService>(
            SourceEventService
        );
        sourceEventService.onEvent(this.onSourceEvent);
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
                const token = await getAuthToken();
                if (token === undefined) {
                    return;
                }
                await setAuthCookie(token);
                return;
        }
    };
}
