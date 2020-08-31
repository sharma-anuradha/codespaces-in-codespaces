import React from 'react';

import { IEnvironment, ILocalEnvironment } from 'vso-client-core';
import { ICredentialsProvider } from 'vscode-web';

import { ServerlessWorkbench } from '../../../vscode/workbenches/serverlessWorkbench';

export interface IServerlessSplashscreenProps {
    environment: IEnvironment | ILocalEnvironment | null;
    credentialsProvider: ICredentialsProvider;
}

export const ServerlessSplashscreen: React.FC<IServerlessSplashscreenProps> = (
    props: IServerlessSplashscreenProps
) => {
    const { environment, credentialsProvider } = props;
    const githubURLString = environment?.seed?.moniker;
    if (!environment || !githubURLString) {
        return <div>Invalid github seed</div>;
    }
    const githubURL = new URL(githubURLString);
    let [org, repository, type, ...rest] = githubURL.pathname.substr(1).split('/');

    var branchOrCommit = 'HEAD';
    if (type === 'commit' || type === 'tree') {
        branchOrCommit = rest.join('/');
    } else if (type === 'pull') {
        // TODO: how to get commit id from PR
    }
    const folderUri = `github://${org}+${repository}+${branchOrCommit}/`;

    return <ServerlessWorkbench folderUri={folderUri} credentialsProvider={credentialsProvider} />;
};
