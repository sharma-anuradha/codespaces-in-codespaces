import {
    vscode,
    UrlCallbackProvider
} from 'vso-workbench';

import { getVSCodeScheme } from 'vso-client-core';

import { getStoredGitHubToken, isGitHubTokenUpdate } from '../services/gitHubAuthenticationService';
import { createUniqueId } from '../dependencies';

export class GitHubUrlCallbackProvider extends UrlCallbackProvider {
    authority: string | undefined;
    path: string | undefined;

    constructor(private readonly scope: string | undefined = undefined) {
        super();
        this.extensionCallbackPath = '/github-auth';
    }

    protected onStorage = (e: StorageEvent) => {
        const { key, newValue } = e;
        if (!key || !newValue) {
            return;
        }

        if (isGitHubTokenUpdate(e)) {
            return;
        }

        if (getStoredGitHubToken(this.scope)) {
            const protocolHandlerUri = vscode.URI.from({
                authority: this.authority,
                scheme: getVSCodeScheme(),
                path: this.path,
            });

            this[this.callbackSymbol].fire(protocolHandlerUri);
        }
    };

    protected generateUrlCallbackParams(authority: string, path: string, query: string) {
        this.authority = authority;
        this.path = path;

        const params = new URLSearchParams();
        params.append('state', encodeURIComponent(createUniqueId()));
        if (this.scope) {
            params.append('scope', this.scope);
        }

        return params.toString();
    }
}
