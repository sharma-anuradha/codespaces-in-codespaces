import * as rpc from 'vscode-jsonrpc';
import { vsls, createTrace, maybePii } from 'vso-client-core';

import { SharedServiceImp } from './SharedService';
import { SupportedGitService } from '../interfaces/SupportedGitService';
import { getSupportedGitServiceByHost } from '../utils/getSupportedGitServiceByHost';
import { parseGitCredentialsFillInput } from '../utils/parseGitCredentialsFillInput';

const serviceName = 'IGitCredentialManager';
const credentialFunction = 'credentialFill';

export const trace = createTrace('GitCredentialService');

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
            vsls.CollectionChangeType.Add
        );

        this.sharedService.onRequest(credentialFunction, this.onRequest.bind(this));
    }

    public async onRequest([input]: string[]) {
        const fillRequest = parseGitCredentialsFillInput(input);

        trace.verbose('Received git credential fill request', maybePii(fillRequest));

        if (fillRequest.protocol === 'https' || fillRequest.protocol === 'http') {
            trace.verbose('Resolving ' + fillRequest.host + ' credential.');

            const token = await this.getTokenByHost(getSupportedGitServiceByHost(fillRequest.host));

            if (token) {
                trace.verbose('Filled credential.', maybePii(fillRequest));

                return `username=${token}\npassword=x-oauth-basic\n`;
            }
        }

        trace.verbose('Failed to fill credential.', maybePii(fillRequest));

        return input;
    }

    public async getTokenByHost(supportedGitService: SupportedGitService): Promise<string | null> {
        return null;
    }
}
