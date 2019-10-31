import { URI, IURLCallbackProvider } from 'vscode-web';

import { vscode } from '../utils/vscode';

import { Emitter, Event } from 'vscode-jsonrpc';

import { getQueryParams } from '../utils/getQueryParams';

import { randomString } from '../utils/randomString';
import { vscodeConfig } from '../constants';

const callbackSymbol = Symbol('URICallbackSymbol');

const VSO_NONCE_PARAM_NAME = 'vso-nonce';

const LOCAL_STORAGE_KEY = 'vsonline.redirect.url';

interface IExpectedNonceRecord {
    authority: string;
    path: string;
}

export class UrlCallbackProvider implements IURLCallbackProvider {
    private [callbackSymbol] = new Emitter<URI>();

    constructor() {
        // listen for changes to localStorage
        if (window.addEventListener) {
            window.addEventListener('storage', this.onStorage, false);
        } else {
            (window as any).attachEvent('onstorage', this.onStorage);
        };
    }

    private expectedNonceMap = new Map<string, IExpectedNonceRecord>();

    private onStorage = (e: StorageEvent) => {
        const { key, newValue } = e;

        if (!key || !newValue) {
            return;
        }

        const [nonce, keyName] = key.split('::');
        
        if (!nonce || (keyName !== LOCAL_STORAGE_KEY)) {
            return;
        }

        const expectedRedirectRecord = this.expectedNonceMap.get(nonce);

        if (!expectedRedirectRecord)
        {
            return;
        }

        localStorage.removeItem(key);
        this.expectedNonceMap.delete(nonce);

        const url = newValue;
        const queryParams = getQueryParams(url);

        // please use the https://github.com/microsoft/vscode/blob/2320972f5f1206d6e9a047e3850b4d0bee4d2e87/src/vs/base/common/uri.ts#L99
        // for the Uri format refrence
        const protocolHandlerUri = vscode.URI.from({
            authority: expectedRedirectRecord.authority,
            query: this.cleanQueryFromVsoParams(queryParams).toString(),
            scheme: this.getVSCodeScheme(),
            path: expectedRedirectRecord.path,
            fragment: ''
        });

        this[callbackSymbol].fire(protocolHandlerUri);
    }

    private getVSCodeScheme() {
        return (vscodeConfig.quality === 'insider')
            ? 'vscode-insiders'
            : 'vscode';
    }

    private cleanQueryFromVsoParams(queryParams: URLSearchParams) {
        const queryParamsWithoutVSOParams = new URLSearchParams(queryParams.toString());

        queryParamsWithoutVSOParams.delete(VSO_NONCE_PARAM_NAME);

        return queryParamsWithoutVSOParams;
    }
    
    public onCallback: Event<URI> = this[callbackSymbol].event;

    private generateUrlCallbackParams(authority: string, path: string, query: string) {
        const nonce = randomString();
        this.expectedNonceMap.set(nonce, { authority, path });

        const params = new URLSearchParams(query);
        params.append(VSO_NONCE_PARAM_NAME, nonce);

        return params.toString();
    }

    public create(options: Partial<URI>) {
        if (!options) {
            throw new Error('No "options" set.');
        }

        const { authority, path = '/', query = '' } = options;

        if (!authority) {
            throw new Error('No "authority" set.');
        }

        const uri = vscode.URI.from({
            scheme: location.protocol.replace(/\:/g, ''),
            path: '/extension-auth-callback',
            authority: location.host,
            query: this.generateUrlCallbackParams(authority!, path, query),
            fragment: options.fragment || ''
        });

        return uri;
    }
}
