import * as path from 'path';
import { IEnvironment } from 'vso-client-core';

import { SupportedGitService, getSupportedGitService } from '../../utils/gitUrlNormalization';

const isGitHubPRUrl = (url: string | undefined) => {
    if ((typeof url !== 'string') || !url) {
        return false;
    }

    if (getSupportedGitService(url) !== SupportedGitService.GitHub) {
        return false;
    }

    return (url.match(/https:\/\/github\.com\/.+\/.+\/pull\/\d+/) !== null);
};

export const getWorkbenchDefaultLayout = (environmentInfo: IEnvironment, isFirstRun: boolean) => {
    if (!isFirstRun) {
        return;
    }

    const githubUrl = environmentInfo.seed?.moniker;

    if (!isGitHubPRUrl(githubUrl)) {
        return;
    }

    const prContainer = {
        // The id of the `viewsContainers.activitybar` contributed by the PR extension
        id: 'github-pull-requests',
        // Sets the activity bar icon to be visible and active
        active: true,
    };

    const sessionPath = environmentInfo?.connection?.sessionPath || '';
    let defaultEditor = {
        path: path.join(sessionPath, 'README.md'),
        scheme: 'vscode-remote',
        active: true,
    };
    
    const containers = prContainer ? [prContainer] : [];
    const editors = defaultEditor ? [defaultEditor] : [];

    const result = {
        firstRun: true,
        sidebar: {
            visible: true,
            containers,
        },
        editors,
        panel: {
            // Sets the panel to be visible
            visible: true,
            containers: [{ id: 'comments', order: 0, active: true }]
        },
    };

    return result;
}
