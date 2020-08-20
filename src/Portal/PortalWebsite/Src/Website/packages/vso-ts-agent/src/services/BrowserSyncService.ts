import { vsls } from 'vso-client-core';

export enum BrowserConnectorMessages {
    ConnectToEnvironment = 'VSO_BrowserSync_ConnectToEnvironment',
    DisconnectFromEnvironment = 'VSO_BrowserSync_DisconnectFromEnvironment',
    OpenPortInBrowser = 'VSO_BrowserSync_OpenPortInBrowser',
    CopyServerUrl = 'VSO_BrowserSync_CopyServerUrl',
    ForwardPort = 'VSO_BrowserSync_ForwardPort',
    SignOut = 'VSO_BrowserSync_SignOut',
    GetLocalStorageValueRequest = 'VSO_BrowserSync_GetLocalStorageValue_Request',
    GetLocalStorageValueResponse = 'VSO_BrowserSync_GetLocalStorageValue_Response',
    IsGoHomeFileMenuItemEnabled = 'vsonline:is-go-home-file-menu-item-enabled',
}

export interface IForwardPortPayload {
    port: number;
}

export class BrowserSyncService {
    public constructor(protected readonly sourceEventService: vsls.SourceEventService) {
        this.sourceEventService.onEvent((e) => {
            this.onSourceEvent(e);
        });
    }

    public async onSourceEvent(e: vsls.SourceEventArgs) {}
}
