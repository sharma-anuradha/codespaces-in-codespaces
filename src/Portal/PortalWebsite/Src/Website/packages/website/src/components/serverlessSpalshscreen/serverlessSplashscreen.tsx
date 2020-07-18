import React from 'react';

import { ILocalEnvironment } from 'vso-client-core';
import { ServerlessWorkbench } from '../serverlessWorkbench/serverlessWorkbench';

export interface IServerlessSplashscreenProps {
    environment: ILocalEnvironment;
}

export const ServerlessSplashscreen: React.FC<IServerlessSplashscreenProps> = (
    props: IServerlessSplashscreenProps
) => {
    const { environment } = props;
    const githubURL = new URL(environment.seed?.moniker);
    let [org, repository, type, ...rest] = githubURL.pathname.substr(1).split('/');

    var branchOrCommit = 'HEAD';
    if (type === 'commit' || type === 'tree') {
        branchOrCommit = rest.join('/');
    } else if (type === 'pull') {
        // TODO: how to get commit id from PR
    }
    const folderUri = `github://${branchOrCommit}/${org}/${repository}`;

    return (
        <div>
            <ServerlessWorkbench folderUri={folderUri} />
        </div>
    );
};
