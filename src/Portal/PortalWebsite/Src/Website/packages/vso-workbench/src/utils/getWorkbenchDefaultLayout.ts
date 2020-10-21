import * as path from 'path';
import { IEnvironment } from 'vso-client-core';

import { IDefaultEditor, IDefaultView, IDefaultLayout } from 'vscode-web';
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

const getViews = (environmentInfo: IEnvironment) => {
    const views: IDefaultView[] = [];

    const githubUrl = environmentInfo.seed?.moniker;
    if (isGitHubPRUrl(githubUrl)) {
        views.push({ id: 'workbench.panel.comments' }, { id: 'prStatus:github' });
    } else {
        views.push({ id: 'workbench.panel.terminal' });
    }

    return views;
}

export const getWorkbenchDefaultLayout = (environmentInfo: IEnvironment) => {
    const result: IDefaultLayout = {
        editors: getEditors(environmentInfo),
        views: getViews(environmentInfo)
    };

    return result;
}
