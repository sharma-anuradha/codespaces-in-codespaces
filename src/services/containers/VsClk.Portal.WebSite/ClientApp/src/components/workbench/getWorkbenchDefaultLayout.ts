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

const getContainers = (environmentInfo: IEnvironment) => {
    const githubUrl = environmentInfo.seed?.moniker;
    if (!isGitHubPRUrl(githubUrl)) {
        return [];
    }

    return [
        {
            // The id of the `viewsContainers.activitybar` contributed by the PR extension
            id: 'github-pull-requests',
            // Sets the activity bar icon to be visible and active
            active: true,
        }
    ];
};

const getEditors = (environmentInfo: IEnvironment) => {
    const sessionPath = environmentInfo.connection?.sessionPath || '';
    
    return [
        {
            path: path.join(sessionPath, 'README.md'),
            scheme: 'vscode-remote',
            active: true,
        }
    ];
};

const getPanel = (environmentInfo: IEnvironment) => {
    const githubUrl = environmentInfo.seed?.moniker;
    if (!isGitHubPRUrl(githubUrl)) {
        return;
    }

    return {
        visible: true,
        containers: [{ id: 'comments', order: 0, active: true }]
    };
}

export const getWorkbenchDefaultLayout = (environmentInfo: IEnvironment, isFirstRun: boolean) => {
    if (!isFirstRun) {
        return;
    }

    const result = {
        firstRun: true,
        sidebar: {
            visible: true,
            containers: getContainers(environmentInfo),
        },
        editors: getEditors(environmentInfo),
        panel: getPanel(environmentInfo),
    };

    return result;
}
