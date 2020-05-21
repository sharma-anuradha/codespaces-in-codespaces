import { IWorkspace, IWorkspaceProvider, URI } from 'vscode-web';
import { IEnvironment } from 'vso-client-core';

import { vscode } from '../../vscodeAssets/vscode';
import { parseWorkspacePayload } from '../../../utils/parseWorkspacePayload';
import { DOGFOOD_CHANNEL_QUERY_PARAM_NAME } from '../../../constants';
import { getUriAuthority } from '../../../utils/getUriAuthority';

const isUntitledWorkspace = (path: string) => {
    return !!path.match(/Untitled\-\d+\.code\-workspace/gim);
};

export class WorkspaceProvider implements IWorkspaceProvider {
    public readonly workspace: IWorkspace;
    public readonly payload?: [string, any][];

    constructor(
        params: URLSearchParams,
        private environmentInfo: IEnvironment,
        private getWorkspaceUrl: (url: URL) => URL
    ) {
        const workspace = params.get('workspace');
        const folder = params.get('folder');
        const isEmpty = params.get('ew');
        const playloadString = params.get('payload');

        this.payload = parseWorkspacePayload(playloadString) || void 0;

        if (isEmpty === 'true') {
            this.workspace = undefined;
        } else if (workspace !== null) {
            let workspaceUri = vscode.URI.parse(workspace);
            /**
             * If no schema present, use the remote authority one
             */
            if (workspaceUri.scheme === 'file') {
                const scheme = (isUntitledWorkspace(workspace))
                    ? 'vscode-userdata'
                    : 'vscode-remote';

                const authority = (isUntitledWorkspace(workspace))
                    ? workspaceUri.authority
                    : `vsonline+${environmentInfo.id}`;

                workspaceUri = vscode.URI.from({
                    ...workspaceUri,
                    scheme,
                    authority,
                });
            }

            this.workspace = { workspaceUri };
        } else {
            const folderUri = vscode.URI.from({
                path: this.normalizeVSCodePath(folder || environmentInfo.connection.sessionPath),
                scheme: 'vscode-remote',
                authority: getUriAuthority(environmentInfo),
            });
            this.workspace = { folderUri };
        }
    }

    public getApplicationUri(quality: string): URI {
        const scheme = quality === 'insider' ? 'vscode-insiders' : 'vscode';
        const uriPrefix = `${scheme}://vscode-remote/vsonline+${this.environmentInfo.id}`;
        let path = '';

        if (!this.workspace) {
            path = this.environmentInfo.connection.sessionPath;
        } else if (this.isFolderToOpen(this.workspace)) {
            path = this.workspace.folderUri.path;
        } else if (this.isWorkspaceToOpen(this.workspace)) {
            path = this.workspace.workspaceUri.path;
        }

        return vscode.URI.parse(uriPrefix + path);
    }

    /**
     * VSCode workbench fails on the windows paths,
     * normalize for this scenario.
     */
    private normalizeVSCodePath(path: string = '') {
        if (!path) {
            return path;
        }

        path = path.trim();

        // replace all backward slashes with forward ones
        // and remove the slash at the beginning
        const forwardSlashPath = path.replace(/\\/g, '/').replace(/^\//, '');

        // add the slash at the beginning
        return `/${forwardSlashPath}`;
    }

    public async open(
        workspace: IWorkspace,
        options: { reuse?: boolean | undefined; payload?: [string, string][] } = {}
    ): Promise<void> {
        const defaultUrl = new URL(document.location.pathname, document.location.origin);
        const currentParams = new URLSearchParams(location.search);

        /**
         * If don't open new tab, use the default url even in GitHub case
         * since we need to change folder/workspace inside the iframe itself.
         * If planning to open a new tab, create the embedder URL.
         */
        const targetUrl = (!options || (options.reuse === true))
            ? defaultUrl
            : this.getWorkspaceUrl(defaultUrl);

        const dogfoodChannelParam = currentParams.get(DOGFOOD_CHANNEL_QUERY_PARAM_NAME)
        if (typeof dogfoodChannelParam === 'string' && dogfoodChannelParam.trim()) {
            targetUrl.searchParams.set(DOGFOOD_CHANNEL_QUERY_PARAM_NAME, dogfoodChannelParam);
        }

        if (!workspace) {
            targetUrl.searchParams.set('ew', 'true');
        } else if (this.isFolderToOpen(workspace)) {
            targetUrl.searchParams.set('folder', workspace.folderUri.path);
        } else if (this.isWorkspaceToOpen(workspace)) {
            targetUrl.searchParams.set('workspace', workspace.workspaceUri.path);
        } else {
            throw new Error('Unsupported workspace type.');
        }

        if (options.payload) {
            targetUrl.searchParams.set('payload', JSON.stringify(options.payload));
        }

        if (!options || options.reuse) {
            window.location.href = targetUrl.href;
            return;
        }

        window.open(targetUrl.href, '_blank', 'noopener, noreferrer');
    }

    private isFolderToOpen(uriToOpen: IWorkspace): uriToOpen is { folderUri: URI } {
        return !!(uriToOpen as { folderUri: URI }).folderUri;
    }

    private isWorkspaceToOpen(uriToOpen: IWorkspace): uriToOpen is { workspaceUri: URI } {
        return !!(uriToOpen as { workspaceUri: URI }).workspaceUri;
    }
}
