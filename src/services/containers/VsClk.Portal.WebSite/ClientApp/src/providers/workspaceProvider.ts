import { IWorkspace, IWorkspaceProvider, URI } from 'vscode-web';
import { vscode } from '../utils/vscode';
import { ICloudEnvironment } from '../interfaces/cloudenvironment';

export class WorkspaceProvider implements IWorkspaceProvider {
    public readonly workspace: IWorkspace;

    constructor(params: URLSearchParams, environmentInfo: ICloudEnvironment) {
        const workspace = params.get('workspace');
        const folder = params.get('folder');
        const isEmpty = params.get('ew');

        if (isEmpty === 'true') {
            this.workspace = undefined;
        } else if (workspace !== null) {
            const workspaceUri = vscode.URI.from({
                path: workspace,
                scheme: 'vscode-remote',
                authority: `vsonline+${environmentInfo.id}`,
            });
            this.workspace = { workspaceUri };
        } else {
            const folderUri = vscode.URI.from({
                path: this.normalizeVSCodePath(folder || environmentInfo.connection.sessionPath),
                scheme: 'vscode-remote',
                authority: `vsonline+${environmentInfo.id}`,
            });
            this.workspace = { folderUri };
        }
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
        const forwardSlashPath = path
            .replace(/\\/g, '/')
            .replace(/^\//, '');

        // add the slash at the beginning
        return `/${forwardSlashPath}`;
    }

    public async open(
        workspace: IWorkspace,
        options?: { reuse?: boolean | undefined } | undefined
    ): Promise<void> {
        const targetUrl = new URL(document.location.pathname, document.location.origin);

        if (!workspace) {
            targetUrl.searchParams.set('ew', 'true');
        } else if (this.isFolderToOpen(workspace)) {
            targetUrl.searchParams.set('folder', workspace.folderUri.path);
        } else if (this.isWorkspaceToOpen(workspace)) {
            targetUrl.searchParams.set('workspace', workspace.workspaceUri.path);
        } else {
            throw new Error('Unsupported workspace type.');
        }

        if (!options || options.reuse) {
            window.location.href = targetUrl.href;
            return;
        }

        window.open(targetUrl.href, '_blank');
    }

    private isFolderToOpen(uriToOpen: IWorkspace): uriToOpen is { folderUri: URI } {
        return !!(uriToOpen as { folderUri: URI }).folderUri;
    }

    private isWorkspaceToOpen(uriToOpen: IWorkspace): uriToOpen is { workspaceUri: URI } {
        return !!(uriToOpen as { workspaceUri: URI }).workspaceUri;
    }
}
