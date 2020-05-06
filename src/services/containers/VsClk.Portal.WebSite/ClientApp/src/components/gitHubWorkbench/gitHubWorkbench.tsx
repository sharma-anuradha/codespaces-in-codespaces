import React, { Component } from 'react';
import { connect } from 'react-redux';
import { RouteComponentProps } from 'react-router-dom';

import { ApplicationState } from '../../reducers/rootReducer';
import { ServerlessWorkbench, RepoType_QueryParam } from '../serverlessWorkbench/serverlessWorkbench';
import { defaultConfig } from '../../services/configurationService';

export interface GitHubWorkbenchProps extends RouteComponentProps<{ id: string }> {
    org: string;
    repoId: string;
    commitId: string;
    filePath: string;
    richNavWebExtensionEndpoint: string;
}

class GitHubWorkbenchView extends Component<GitHubWorkbenchProps, GitHubWorkbenchProps> {
    render() {
        const extensionUrls = [this.props.richNavWebExtensionEndpoint];

         // Repo Info to pass to Rich Code Nav should be stored in the workspace URI
         const uriQueryObj = {
            repoType: RepoType_QueryParam.GitHub,
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
    const { richNavWebExtensionEndpoint } = state.configuration || defaultConfig;

    return {
        org,
        repoId,
        commitId,
        filePath,
        richNavWebExtensionEndpoint,
    };
};

export const GitHubWorkbench = connect(getProps)(GitHubWorkbenchView);
