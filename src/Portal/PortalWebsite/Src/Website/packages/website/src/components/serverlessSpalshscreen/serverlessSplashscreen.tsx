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
    let [org, repository] = githubURL.pathname.substr(1).split('/');
    if (repository.endsWith('.git')) {
        repository = repository.substr(0, repository.length - '.git'.length);
    }

    const uriQueryObj = [
        { key: 'environmentId', value: environment.id ?? '' },
        { key: 'org', value: org },
        { key: 'repoId', value: repository },
    ];

    const uriQueryString = uriQueryObj
        .map((item) => encodeURIComponent(item.key) + '=' + encodeURIComponent(item.value))
        .join('&');
    const folderUri = `vscs-githubfs:/GitHubFS/?${uriQueryString}`;

    const extensionUrls = [
        'https://vscsextensionsdev.blob.core.windows.net/vsonline-fsprovider-extension',
    ];
    return (
        <div>
            <ServerlessWorkbench folderUri={folderUri} extensionUrls={extensionUrls} />
        </div>
    );
};
