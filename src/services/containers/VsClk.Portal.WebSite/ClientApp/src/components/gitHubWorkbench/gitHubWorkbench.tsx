import React, { Component } from 'react';
import { connect } from 'react-redux';
import { RouteComponentProps } from 'react-router-dom';

import { ApplicationState } from '../../reducers/rootReducer';
import { ServerlessWorkbench } from '../serverlessWorkbench/serverlessWorkbench';
import { PageNotFound } from '../pageNotFound/pageNotFound';

export interface GitHubWorkbenchProps extends RouteComponentProps<{ id: string }> {
    org: string;
    repoId: string;
    commitId: string;
    filePath: string;
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

         // Repo Info to pass to Rich Code Nav should be stored in the workspace URI
         const uriQueryObj = {
            org: this.props.org,
            repoId: this.props.repoId,
            commitId: this.props.commitId,
            filePath: this.props.filePath
        };
        const uriQueryString = JSON.stringify(uriQueryObj);
        const folderUri = `vsck:/Rich Code Navigation/?${uriQueryString}`;
        
        return <ServerlessWorkbench
                    folderUri={folderUri}
                    extensionUrls={extensionUrls}
                />;
    }
}

const getProps = (
    state: ApplicationState,
    props: RouteComponentProps<{ org: string; repoId: string, commitId: string }>
) => {
    const org = props.match.params.org;
    const repoId = props.match.params.repoId;
    const commitId = props.match.params.commitId;

    const fileParam = new URLSearchParams(props.location.search).get("filePath");
    const filePath = fileParam ? fileParam : "";

    return {
        org,
        repoId,
        commitId,
        filePath
    };
};

export const GitHubWorkbench = connect(getProps)(GitHubWorkbenchView);
