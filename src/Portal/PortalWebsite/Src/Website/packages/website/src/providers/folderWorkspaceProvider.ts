import { IWorkspace, IWorkspaceProvider, URI } from 'vscode-web';
import { vscode } from 'vso-workbench';

export class FolderWorkspaceProvider implements IWorkspaceProvider {
    constructor(
        private folderUri: string,
        private readonly targetURLFactory?: (folderUri: URI) => URL | undefined
    ) {}

    public workspace: IWorkspace = {
        folderUri: vscode.URI.parse(this.folderUri),
    };

    public async open(
        workspace: IWorkspace,
        options?: { reuse?: boolean | undefined } | undefined
    ): Promise<void> {
        const folder = workspace as { folderUri: URI };
        if (folder && folder.folderUri && this.targetURLFactory) {
            const targetUrl = this.targetURLFactory(folder.folderUri);
            if (targetUrl) {
                if (!options || options.reuse) {
                    window.location.href = targetUrl.href;
                    return;
                }

                window.open(targetUrl.href, '_blank');
            }
        }
    }
}
