import { IUriComponents } from './IUriComponents';

import { Event } from 'vscode-jsonrpc';

export interface IURLCallbackProvider {

    /**
     * Indicates that a Uri has been opened outside of VSCode. The Uri
     * will be forwarded to all installed Uri handlers in the system.
     */
    readonly onCallback: Event<IUriComponents>;

    /**
     * Creates a Uri that - if opened in a browser - must result in
     * the `onCallback` to fire.
     *
     * The optional `Partial<UriComponents>` must be properly restored for
     * the Uri passed to the `onCallback` handler.
     *
     * For example: if a Uri is to be created with `scheme:"vscode"`,
     * `authority:"foo"` and `path:"bar"` the `onCallback` should fire
     * with a Uri `vscode://foo/bar`.
     *
     * If there are additional `query` values in the Uri, they should
     * be added to the list of provided `query` arguments from the
     * `Partial<UriComponents>`.
     */
    create(options?: Partial<IUriComponents>): IUriComponents;
}
