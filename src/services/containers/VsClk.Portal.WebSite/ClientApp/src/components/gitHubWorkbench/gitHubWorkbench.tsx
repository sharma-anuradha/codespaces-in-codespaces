import React, { Component } from 'react';
import { connect } from 'react-redux';
import { RouteComponentProps } from 'react-router-dom';

import { ApplicationState } from '../../reducers/rootReducer';
import { ServerlessWorkbench } from '../serverlessWorkbench/serverlessWorkbench';
import { PageNotFound } from '../pageNotFound/pageNotFound';

export interface GitHubWorkbenchProps extends RouteComponentProps<{ id: string }> {
    owner: string;
    repo: string;
}

class GitHubWorkbenchView extends Component<GitHubWorkbenchProps, GitHubWorkbenchProps> {
    render() {
        // Enable this only for dev currently while we explore the idea.
        if (!window.location.hostname.includes('online.dev')) {
            return <PageNotFound />;
        }

        const extensionUrls = [
            'https://testrichcodenavext.blob.core.windows.net/richnavext/vscode-lsif-browser',
        ];

        // This is the folder URI format recognized by the RichNav file system provider.
        // TODO: What's the format for passing in owner/repo?
        // const folderUri = `vsck://RichCodeNav/${this.props.owner}/${this.props.repo}
        const folderUri = `vsck://RichCodeNav/`;

        return <ServerlessWorkbench folderUri={folderUri} extensionUrls={extensionUrls} />;
    }
}

const getProps = (
    state: ApplicationState,
    props: RouteComponentProps<{ owner: string; repo: string }>
) => {
    const owner = props.match.params.owner;
    const repo = props.match.params.repo;

    return {
        owner,
        repo,
    };
};

export const GitHubWorkbench = connect(getProps)(GitHubWorkbenchView);
