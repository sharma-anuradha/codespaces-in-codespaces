import { IWorkspace, IWorkspaceProvider, URI } from 'vscode-web';
import { vscode } from '../utils/vscode';

export class WorkspaceProvider implements IWorkspaceProvider {
    public readonly workspace: IWorkspace;

    constructor(params: URLSearchParams, sessionPath: string) {
        const workspace = params.get('workspace');
        const folder = params.get('folder');
        const isEmpty = params.get('ew');

        if (isEmpty === 'true') {
            this.workspace = undefined;
        } else if (workspace !== null) {
            const workspaceUri = vscode.URI.from({
                path: workspace,
                scheme: 'vscode-remote',
                authority: `localhost`,
            });
            this.workspace = { workspaceUri };
        } else {
            const folderUri = vscode.URI.from({
                path: folder || sessionPath,
                scheme: 'vscode-remote',
                authority: `localhost`,
            });
            this.workspace = { folderUri };
        }
    }

    public async open(
        workspace: IWorkspace,
        options?: { reuse?: boolean | undefined } | undefined
    ): Promise<void> {
        let targetHref: string | undefined = undefined;

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
