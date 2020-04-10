import * as rpc from 'vscode-jsonrpc';
import { vsls } from 'vso-client-core';

import { SharedServiceImp } from './SharedService';

const serviceName = 'IGitCredentialManager';
const credentialFunction = 'credentialFill';

export class GitCredentialService {
    private workspaceService: vsls.WorkspaceService;
    private rpcConnection: rpc.MessageConnection;
    protected sharedService?: SharedServiceImp;

    constructor(service: vsls.WorkspaceService, connection: rpc.MessageConnection) {
        this.workspaceService = service;
        this.rpcConnection = connection;
    }

    public async shareService(): Promise<void> {
        this.sharedService = new SharedServiceImp(serviceName, this.rpcConnection);

        await this.workspaceService.registerServicesAsync(
            [serviceName],
            vsls.CollectionChangeType.Add,
        );

        this.sharedService.onRequest(credentialFunction, this.onRequest.bind(this));
    }

    public async onRequest([input]: string[]) { return input; }
}

export type GitCredentialsRequest = {
    /**
     * The protocol over which the credential will be used (e.g., https).
     */
    protocol?: string;

    /**
     * The protocol over which the credential will be used (e.g., https).
     */
    host?: string;

    /**
     * The protocol over which the credential will be used (e.g., https).
     */
    path?: string;

    /**
     * The credential’s username, if we already have one (e.g., from a URL, from the user, or from a previously run helper).
     */
    username?: string;

    /**
     * The credential’s password, if we are asking it to be stored.
     */
    password?: string;

    /**
     * The protocol over which the credential will be used (e.g., https).
     */
    url?: string;
};
