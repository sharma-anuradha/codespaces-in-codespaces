import { IApplicationLink } from 'vscode-web';
import { getVSCodeVersion } from '../../../utils/getVSCodeVersion';
import { WorkspaceProvider } from '../workspaceProvider/workspaceProvider';

export const applicationLinksProviderFactory = (
    workspaceProvider: WorkspaceProvider
): IApplicationLink[] => {
    const vscodeConfig = getVSCodeVersion();

    const link: IApplicationLink = {
        uri: workspaceProvider.getApplicationUri(vscodeConfig.quality),
        label: 'Open in Desktop',
    };

    const applicationLinks = [link];

    return applicationLinks;
};
