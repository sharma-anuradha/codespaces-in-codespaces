import { URI, IURLCallbackProvider } from 'vscode-web';

import { vscode } from '../utils/vscode';

import { Emitter, Event } from 'vscode-jsonrpc';

import { getQueryParams } from '../utils/getQueryParams';

import { randomStr } from '../utils/randomStr';
import { vscodeConfig } from '../constants';

const callbackSymbol = Symbol('URICallbackSymbol');

const VSO_AUTHORITY_PARAM_NAME = 'vso-authority';
const VSO_PATH_PARAM_NAME = 'vso-path';
const VSO_NONCE_PARAM_NAME = 'vso-nonce';

const LOCAL_STORAGE_KEY = 'vsonline.redirect.url';

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

    private expectedNonceSet = new Set<string>();

    private onStorage = (e: StorageEvent) => {
        const { key, newValue } = e;

        if (!key || !newValue) {
            return;
        }

        const [nonce, keyName] = key.split('::');

        if (!nonce
            || !this.expectedNonceSet.has(nonce)
            || (keyName !== LOCAL_STORAGE_KEY))
        {
            return;
        }

        localStorage.removeItem(key);
        this.expectedNonceSet.delete(nonce);

        const url = newValue;
        const queryParams = getQueryParams(url);

        if (!queryParams.get(VSO_AUTHORITY_PARAM_NAME)) {
            throw new Error('No "authority" param is set on redirect URL.');
        }

        if (!queryParams.get(VSO_PATH_PARAM_NAME)) {
            throw new Error('No "path" param is set on redirect URL.');
        }

        // please use the https://github.com/microsoft/vscode/blob/2320972f5f1206d6e9a047e3850b4d0bee4d2e87/src/vs/base/common/uri.ts#L99
        // for the Uri format refrence
        const protocolHandlerUri = vscode.URI.with({
            authority: queryParams.get(VSO_AUTHORITY_PARAM_NAME)!,
            query: this.cleanQueryFromVsoParams(queryParams).toString(),
            scheme: this.getVSCodeScheme(),
            path: queryParams.get(VSO_PATH_PARAM_NAME)!,
            fragment: ''
        });

        // keeping this console in to demostrate the issue to the VSCode team
        // tslint:=next-line: no-console
        console.log(`Firing [UrlCallbackProvider.onCallback] with the next argument: `, protocolHandlerUri);

        this[callbackSymbol].fire(protocolHandlerUri);
    }

    private getVSCodeScheme() {
        return (vscodeConfig.quality === 'insider')
            ? 'vscode-insiders'
            : 'vscode';
    }

    private cleanQueryFromVsoParams(queryParams: URLSearchParams) {
        const queryParamsWithoutVSOParams = new URLSearchParams(queryParams.toString());

        queryParamsWithoutVSOParams.delete(VSO_AUTHORITY_PARAM_NAME);
        queryParamsWithoutVSOParams.delete(VSO_PATH_PARAM_NAME);
        queryParamsWithoutVSOParams.delete(VSO_NONCE_PARAM_NAME);

        return queryParamsWithoutVSOParams;
    }
    
    public onCallback: Event<URI> = this[callbackSymbol].event;

    private generateUrlCallbackParams(authority: string, path: string, query: string) {
        const nonce = randomStr();
        this.expectedNonceSet.add(nonce);

        const params = new URLSearchParams(query);
        params.append(VSO_AUTHORITY_PARAM_NAME, authority);
        params.append(VSO_PATH_PARAM_NAME, path);
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

        const uri = vscode.URI.with({
            scheme: location.protocol.replace(/\:/g, ''),
            path: '/extension-auth-callback',
            authority: location.host,
            query: this.generateUrlCallbackParams(authority!, path, query),
            fragment: options.fragment || ''
        })

        return uri;
    }
}
