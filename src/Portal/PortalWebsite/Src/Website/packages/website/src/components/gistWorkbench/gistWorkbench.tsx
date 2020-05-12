import React, { Component } from 'react';
import { RouteComponentProps } from 'react-router-dom';
import { URI } from 'vscode-web';

import { ServerlessWorkbench } from '../serverlessWorkbench/serverlessWorkbench';
import { GitHubUrlCallbackProvider } from '../../providers/gitHubUrlCallbackProvider';
import { PageNotFound } from '../pageNotFound/pageNotFound';

type GistWorkbenchProps = RouteComponentProps<{ id: string }>;

export class GistWorkbench extends Component<GistWorkbenchProps, GistWorkbenchProps> {
    targetURLFactory: (folderUri: URI) => URL | undefined;
    constructor(props: GistWorkbenchProps) {
        super(props);

        this.targetURLFactory = (folderUri: URI) => {
            if (folderUri.scheme == 'gist') {
                // If we are asked to open a workspace, the authority of the folderUri is the gistId.
                return new URL(`gist/${folderUri.authority}`, document.location.origin);
            }
        };
    }

    render() {
        // Enable this only for dev currently while we explore the idea.
        if (!window.location.hostname.includes('online.dev')) {
            return <PageNotFound />;
        }

        const extensionUrls = ['https://gistpadextension.blob.core.windows.net/gistpadext'];

        // This is the folder URI format recognized by the GistPad file system provider.
        const folderUri = `gist://${this.props.match.params.id}/`;
        const urlCallbackProvider = new GitHubUrlCallbackProvider('gist');

        return (
            <ServerlessWorkbench
                folderUri={folderUri}
                targetURLFactory={this.targetURLFactory}
                extensionUrls={extensionUrls}
                urlCallbackProvider={urlCallbackProvider}
            />
        );
    }
}
