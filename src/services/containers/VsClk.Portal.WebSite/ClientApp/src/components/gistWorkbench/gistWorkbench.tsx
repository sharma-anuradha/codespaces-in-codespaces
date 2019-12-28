import React, { Component } from 'react';
import { connect } from 'react-redux';
import { RouteComponentProps } from 'react-router-dom';

import { ApplicationState } from '../../reducers/rootReducer';
import { ServerlessWorkbench } from '../serverlessWorkbench/serverlessWorkbench';
import { GitHubUrlCallbackProvider } from '../../providers/gitHubUrlCallbackProvider';
import { PageNotFound } from '../pageNotFound/pageNotFound';
import { URI } from 'vscode-web';

export interface GistWorkbenchProps extends RouteComponentProps<{ id: string }> {
    gistId: string;
}

class GistWorkbenchView extends Component<GistWorkbenchProps, GistWorkbenchProps> {
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
        if (process.env.NODE_ENV !== 'development') {
            return <PageNotFound />;
        }

        const extensionUrls = ['https://gistpadextension.blob.core.windows.net/gistpadext'];

        // This is the folder URI format recognized by the GistPad file system provider.
        const folderUri = `gist://${this.props.gistId}/`;
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

const getProps = (state: ApplicationState, props: RouteComponentProps<{ id: string }>) => {
    const gistId = props.match.params.id;

    return {
        gistId,
    };
};

export const GistWorkbench = connect(getProps)(GistWorkbenchView);
