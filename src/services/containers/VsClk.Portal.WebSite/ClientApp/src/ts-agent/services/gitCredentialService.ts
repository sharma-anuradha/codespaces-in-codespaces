import * as vsls from '../contracts/VSLS';
import * as rpc from 'vscode-jsonrpc';
import { SharedServiceImp } from './sharedService';
import { createUniqueId } from '../../dependencies';
import { createTrace, maybePii } from '../../utils/createTrace';
import { Signal } from '../../utils/signal';

const trace = createTrace('GitCredentialService');

const serviceName = 'IGitCredentialManager';
const credentialFunction = 'credentialFill';
const localStorageKey = 'githubAccessToken';

type GitHubAccessTokenResponse = {
    readonly accessToken: string;
    readonly state: string;
};

export function storeGitHubAccessTokenResponse({ accessToken, state }: GitHubAccessTokenResponse) {
    localStorage.setItem(localStorageKey, JSON.stringify({ accessToken, state }));
}

export function getStoredGitHubAccessTokenResponse(): GitHubAccessTokenResponse | null {
    const storedTokenString = localStorage.getItem(localStorageKey);

    if (!storedTokenString) {
        return null;
    }

    try {
        const parsedToken = JSON.parse(storedTokenString);
        if (typeof parsedToken.accessToken !== 'string') {
            localStorage.removeItem(localStorageKey);
            return null;
        }
        if (typeof parsedToken.state !== 'string') {
            localStorage.removeItem(localStorageKey);
            return null;
        }

        return parsedToken;
    } catch {
        return null;
    }
}

export function clearGitHubAccessTokenResponse() {
    localStorage.removeItem(localStorageKey);
}

export class GitCredentialService {
    private workspaceService: vsls.WorkspaceService;
    private rpcConnection: rpc.MessageConnection;
    private sharedService?: SharedServiceImp;

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

        this.sharedService.onRequest(credentialFunction, async ([input]: string[]) => {
            const fillRequest = parseGitCredentialsFillInput(input);

            trace.verbose('Received git credential fill request', maybePii(fillRequest));

            if (fillRequest.protocol === 'https' && fillRequest.host === 'github.com') {
                trace.verbose('Resolving GitHub credential.');
                let credentials = getGitHubCredentials();

                if (credentials) {
                    trace.verbose('Filled credential.', maybePii(fillRequest));

                    return credentials;
                }
            }

            trace.verbose('Failed to fill credential.', maybePii(fillRequest));

            return input;
        });
    }
}

export async function getToken(): Promise<string | null> {
    let token = getStoredGitHubToken();
    if (!token) {
        const originalState = createUniqueId();
        try {
            const result = await authenticateAgainstGitHub(originalState);
            if (result && originalState === result.state) {
                token = result.accessToken;
            } else {
                clearGitHubAccessTokenResponse();
            }
        } catch (err) {
            trace.error('Failed to acquire GitHub credentials.', err.message);
        }
    }

    return token;
}

export async function getGitHubCredentials(): Promise<string | null> {
    let token = await getToken();

    if (token) {
        return `username=${token}\npassword=x-oauth-basic\nquit=true\n`;
    }

    return null;
}

export function getStoredGitHubToken(): string | null {
    const storedToken = getStoredGitHubAccessTokenResponse();

    return storedToken && storedToken.accessToken;
}

let tokenRequest: Signal<{ accessToken: string; state: string } | null> | undefined;

function authenticateAgainstGitHub(
    state: string
): Promise<{ accessToken: string; state: string } | null> {
    if (tokenRequest) {
        tokenRequest.cancel();
    }

    const currentTokenRequest = new Signal<{ accessToken: string; state: string } | null>();
    tokenRequest = currentTokenRequest;

    window.open(
        `${window.location.origin}/github-auth?state=${encodeURIComponent(state)}`,
        '_blank'
    );

    const timeout = setTimeout(() => {
        tokenRequest = undefined;
        currentTokenRequest.reject(
            new Error('Failed to acquire GitHub credentials. Reason: timeout.')
        );
    }, 5 * 60 * 1000);

    currentTokenRequest.promise.finally(() => {
        clearTimeout(timeout);
    });

    const resolveWithToken = (event: StorageEvent) => {
        if (event.key === localStorageKey) {
            window.removeEventListener('storage', resolveWithToken);

            clearTimeout(timeout);
            tokenRequest = undefined;

            currentTokenRequest.complete(getStoredGitHubAccessTokenResponse());
        }
    };

    window.addEventListener('storage', resolveWithToken);

    return currentTokenRequest.promise;
}

type GitCredentialsRequest = {
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
