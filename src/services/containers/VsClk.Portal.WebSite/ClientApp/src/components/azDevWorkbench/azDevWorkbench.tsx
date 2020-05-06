import React, { Component } from 'react';
import { connect } from 'react-redux';
import { RouteComponentProps } from 'react-router-dom';
import { getAzDevCredentials } from '../../actions/getAzDevCredentials';
import { ApplicationState } from '../../reducers/rootReducer';
import { defaultConfig } from '../../services/configurationService';
import { Loader } from '../loader/loader';
import { PageNotFound } from '../pageNotFound/pageNotFound';
import { RepoType_QueryParam, ServerlessWorkbench } from '../serverlessWorkbench/serverlessWorkbench';

export interface AzDevWorkbenchProps extends RouteComponentProps<{ id: string }> {
    org: string;
    projectName: string;
    repoName: string;
    filePath: string;
    commitId: string;
    richNavWebExtensionEndpoint: string;
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

        const extensionUrls = [this.props.richNavWebExtensionEndpoint];

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
    const { richNavWebExtensionEndpoint } = state.configuration || defaultConfig;

    return {
        org,
        projectName,
        repoName,
        filePath,
        commitId,
        richNavWebExtensionEndpoint,
    };
};

export const AzDevWorkbench = connect(getProps)(AzDevWorkbenchView);
