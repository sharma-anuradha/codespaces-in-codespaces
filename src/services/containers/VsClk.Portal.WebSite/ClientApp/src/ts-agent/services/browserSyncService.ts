import { SourceEventService, SourceEventArgs } from '../contracts/VSLS';
import { WorkspaceClient } from '../workspaceClient';
import { authService } from '../../services/authService';

export enum BrowserConnectorMessages {
    ConnectToEnvironment = 'VSO_BrowserSync_ConnectToEnvironment',
    DisconnectFromEnvironment = 'VSO_BrowserSync_DisconnectFromEnvironment',
    OpenPortInBrowser = 'VSO_BrowserSync_OpenPortInBrowser',
    CopyServerUrl = 'VSO_BrowserSync_CopyServerUrl',
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
                this.redirect(`/environments`);
                return;
            }
            case BrowserConnectorMessages.SignOut: {
                await authService.signOut();
                this.redirect(`/welcome`);
                return;
            }
            case BrowserConnectorMessages.CopyServerUrl:
                // implement the copy shared server URL logic
                return;
            case BrowserConnectorMessages.OpenPortInBrowser: {
                // implement the open shared server URL logic
                return;
            }
        }
    };
}
