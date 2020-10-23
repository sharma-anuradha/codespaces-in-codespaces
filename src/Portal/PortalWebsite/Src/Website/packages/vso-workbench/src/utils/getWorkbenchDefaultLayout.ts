import { IEnvironment } from 'vso-client-core';

import { IDefaultView, IDefaultLayout } from 'vscode-web';
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

const getViews = (environmentInfo: IEnvironment) => {
    const views: IDefaultView[] = [];

    const githubUrl = environmentInfo.seed?.moniker;
    if (isGitHubPRUrl(githubUrl)) {
        views.push({ id: 'workbench.panel.comments' }, { id: 'prStatus:github' });
    } else {
        views.push({ id: 'terminal' });
    }

    return views;
}

export const getWorkbenchDefaultLayout = (environmentInfo: IEnvironment) => {
    const result: IDefaultLayout = {
        views: getViews(environmentInfo)
    };

    return result;
}
