import { vscode } from '../utils/vscode';
import {
    getStoredGitHubToken,
    gitHubLocalStorageKey,
} from '../services/gitHubAuthenticationService';
import { createUniqueId } from '../dependencies';
import { UrlCallbackProvider, callbackSymbol } from './urlCallbackProvider';

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

        if (key !== gitHubLocalStorageKey) {
            return;
        }

        if (getStoredGitHubToken(this.scope)) {
            const protocolHandlerUri = vscode.URI.from({
                authority: this.authority,
                scheme: this.getVSCodeScheme(),
                path: this.path,
            });

            this[callbackSymbol].fire(protocolHandlerUri);
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
