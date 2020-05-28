import * as rpc from 'vscode-jsonrpc';
import { vsls, createTrace, maybePii } from 'vso-client-core';

import { SharedServiceImp } from './SharedService';
import { SupportedGitService } from '../interfaces/SupportedGitService';
import { getSupportedGitServiceByHost } from '../utils/getSupportedGitServiceByHost';

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
            vsls.CollectionChangeType.Add,
        );

        this.sharedService.onRequest(credentialFunction, this.onRequest.bind(this));
    }

    public async onRequest([input]: string[]) {
        const fillRequest = parseGitCredentialsFillInput(input);

        trace.verbose('Received git credential fill request', maybePii(fillRequest));

        if (fillRequest.protocol === 'https' || fillRequest.protocol === 'http') {
            trace.verbose('Resolving ' + fillRequest.host + ' credential.');

            const token = await this.getTokenByHost(
                getSupportedGitServiceByHost(fillRequest.host)
            );

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


function parseGitCredentialsFillInput(str: string): GitCredentialsRequest {
    // Git asks for credentials in form of string request
    // E.g:
    //      protocol=https
    //      host=github.com
    //
    // https://git-scm.com/docs/git-credential#IOFMT
    //

    const lines = str.split('\n');
    let result: GitCredentialsRequest = {};
    return lines.reduce((parsedInput: any, line) => {
        const [key, value] = getKeyValuePair(line);

        if (key) {
            parsedInput[key] = value;
        }

        return parsedInput;
    }, result);

    function getKeyValuePair(line: string) {
        const delimiterIndex = line.indexOf('=');
        if (delimiterIndex <= 0) {
            return [];
        }
        return [line.slice(0, delimiterIndex), line.slice(delimiterIndex + 1)];
    }
}
