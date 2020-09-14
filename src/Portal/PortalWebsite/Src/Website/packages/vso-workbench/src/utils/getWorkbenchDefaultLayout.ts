import * as path from 'path';
import { IEnvironment } from 'vso-client-core';

import { IDefaultEditor, IDefaultLayout } from 'vscode-web';
import { SupportedGitService, getSupportedGitService } from 'vso-ts-agent';

import { vscode } from '../vscode/vscodeAssets/vscode';
import { getUriAuthority } from './getUriAuthority';

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

    const authority = getUriAuthority(environmentInfo);
    const fsPath = path.join(sessionPath, 'README.md');

    const readmeEditor: IDefaultEditor = {
        uri: vscode.URI.from({
            scheme: 'vscode-remote',
            authority,
            // URI constructor requires the `path` component to start with `/`
            path: path.join('/', fsPath),
        }),
        openOnlyIfExists: true,
    };

    return [
        readmeEditor,
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

export const getWorkbenchDefaultLayout = (environmentInfo: IEnvironment) => {
    const result: IDefaultLayout = {
        sidebar: {
            visible: true,
            containers: getContainers(environmentInfo),
        },
        editors: getEditors(environmentInfo),
        panel: getPanel(environmentInfo),
    };

    return result;
}
