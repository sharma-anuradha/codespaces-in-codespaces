import React, { Component } from 'react';
import { connect, ConnectedComponent } from 'react-redux';
import { RouteComponentProps } from 'react-router-dom';

import { ApplicationState } from '../../reducers/rootReducer';
import {
    ServerlessWorkbench,
    RepoType_QueryParam,
} from '../serverlessWorkbench/serverlessWorkbench';
import { defaultConfig } from '../../services/configurationService';

type Params = { org: string; repoId: string; commitId: string };

export interface GitHubWorkbenchProps extends RouteComponentProps<Params> {
    org: string;
    repoId: string;
    commitId: string;
    filePath: string;
    richNavWebExtensionEndpoint: string;
}

const CODETOUR_ENDPOINT = 'https://vscsextensionsdev.blob.core.windows.net/codetour';

class GitHubWorkbenchView extends Component<GitHubWorkbenchProps, GitHubWorkbenchProps> {
    render() {
        var extensionUrls: string[] = [CODETOUR_ENDPOINT];
        var commitId = this.props.commitId ? this.props.commitId : 'HEAD';
        var folderUri = `github://${commitId}/${this.props.org}/${this.props.repoId}`;

        if (localStorage.getItem('vscs-showserverless') !== 'true') {
            extensionUrls = [this.props.richNavWebExtensionEndpoint, CODETOUR_ENDPOINT];

            // Repo Info to pass to Rich Code Nav should be stored in the workspace URI
            const uriQueryObj = {
                repoType: RepoType_QueryParam.GitHub,
                org: this.props.org,
                repoId: this.props.repoId,
                commitId: this.props.commitId,
                filePath: this.props.filePath,
            };
            const uriQueryString = JSON.stringify(uriQueryObj);
            folderUri = `vsck:/Rich Code Navigation/?${uriQueryString}`;
        }

        return <ServerlessWorkbench folderUri={folderUri} extensionUrls={extensionUrls} />;
    }
}
const getProps: (
    state: ApplicationState,
    props: RouteComponentProps<Params>
) => Omit<GitHubWorkbenchProps, keyof RouteComponentProps<Params>> = (state, props) => {
    const org = props.match.params.org;
    const repoId = props.match.params.repoId;
    const commitId = props.match.params.commitId;

    const fileParam = new URLSearchParams(props.location.search).get('filePath');
    const filePath = fileParam ? fileParam : '';
    const { richNavWebExtensionEndpoint } = state.configuration || defaultConfig;

    return {
        org,
        repoId,
        commitId,
        filePath,
        richNavWebExtensionEndpoint,
    };
};

type MappedProperties = keyof ReturnType<typeof getProps>;

export const GitHubWorkbench: ConnectedComponent<
    typeof GitHubWorkbenchView,
    Omit<GitHubWorkbenchProps, MappedProperties> & RouteComponentProps<Params>
> = connect(getProps)(GitHubWorkbenchView);
