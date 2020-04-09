import React, { Component } from 'react';
import { connect } from 'react-redux';
import { RouteComponentProps } from 'react-router-dom';

import { ApplicationState } from '../../reducers/rootReducer';
import { ServerlessWorkbench, RepoType_QueryParam } from '../serverlessWorkbench/serverlessWorkbench';
import { PageNotFound } from '../pageNotFound/pageNotFound';
import { Loader } from '../loader/loader';
import { getAzDevCredentials } from '../../actions/getAzDevCredentials';

export interface AzDevWorkbenchProps extends RouteComponentProps<{ id: string }> {
    org: string;
    projectName: string;
    repoName: string;
    filePath: string;
    commitId: string;
}

export interface AzDevWorkbenchState {
    loading: boolean;
}

class AzDevWorkbenchView extends Component<AzDevWorkbenchProps, AzDevWorkbenchState> {
    constructor(props: AzDevWorkbenchProps) {
        super(props);

        this.state = {
            loading: true,
        };

        getAzDevCredentials().then(() => {
            this.setState({
                loading: false,
            });
        });
    }

    render() {
        // Enable this only for dev currently while we explore the idea.
        if (!window.location.hostname.includes('online.dev')) {
            return <PageNotFound />;
        }

        if (this.state.loading) {
            return <Loader message='Logging into Azure DevOps...'></Loader>;
        }

        const extensionUrls = [
            'https://testrichcodenavext.blob.core.windows.net/richnavext/vscode-lsif-browser',
        ];

        // Repo Info to pass to Rich Code Nav should be stored in the workspace URI
        const uriQueryObj = {
            repoType: RepoType_QueryParam.AzureDevOps,
            org: this.props.org,
            projectName: this.props.projectName,
            repoName: this.props.repoName,
            filePath: this.props.filePath,
            commitId: this.props.commitId,
        };
        const uriQueryString = JSON.stringify(uriQueryObj);
        const folderUri = `vsck:/Rich Code Navigation/?${uriQueryString}`;

        return <ServerlessWorkbench folderUri={folderUri} extensionUrls={extensionUrls} />;
    }
}

const getProps = (
    state: ApplicationState,
    props: RouteComponentProps<{
        org: string;
        projectName: string;
        repoName: string;
        commitId: string;
    }>
) => {
    const org = props.match.params.org;
    const projectName = props.match.params.projectName;
    const repoName = props.match.params.repoName;
    const commitId = props.match.params.commitId;

    const fileParam = new URLSearchParams(props.location.search).get('filePath');
    const filePath = fileParam ? fileParam : '';

    return {
        org,
        projectName,
        repoName,
        filePath,
        commitId,
    };
};

export const AzDevWorkbench = connect(getProps)(AzDevWorkbenchView);
