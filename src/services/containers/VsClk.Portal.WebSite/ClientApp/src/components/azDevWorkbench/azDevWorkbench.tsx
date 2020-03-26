import React, { Component } from 'react';
import { connect } from 'react-redux';
import { RouteComponentProps } from 'react-router-dom';

import { ApplicationState } from '../../reducers/rootReducer';
import { ServerlessWorkbench } from '../serverlessWorkbench/serverlessWorkbench';
import { PageNotFound } from '../pageNotFound/pageNotFound';
import { Loader } from '../loader/loader';
import { getAzDevCredentials } from '../../actions/getAzDevCredentials';

export interface AzDevWorkbenchProps extends RouteComponentProps<{ id: string }> {
    org: string;
    project: string;
    repo: string;
    filePath: string;
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
            repoType: 'AzureDevOps',
            org: this.props.org,
            project: this.props.project,
            repo: this.props.repo,
            filePath: this.props.filePath,
        };
        const uriQueryString = JSON.stringify(uriQueryObj);
        const folderUri = `vsck:/Rich Code Navigation/?${uriQueryString}`;

        return <ServerlessWorkbench folderUri={folderUri} extensionUrls={extensionUrls} />;
    }
}

const getProps = (
    state: ApplicationState,
    props: RouteComponentProps<{ org: string; project: string; repo: string }>
) => {
    const org = props.match.params.org;
    const project = props.match.params.project;
    const repo = props.match.params.repo;

    const fileParam = new URLSearchParams(props.location.search).get('filePath');
    const filePath = fileParam ? fileParam : '';

    return {
        org,
        project,
        repo,
        filePath,
    };
};

export const AzDevWorkbench = connect(getProps)(AzDevWorkbenchView);
