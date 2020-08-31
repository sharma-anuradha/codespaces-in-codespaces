import React, { Component } from 'react';
import { connect, ConnectedComponent } from 'react-redux';
import { RouteComponentProps } from 'react-router-dom';
import {
    ServerlessWorkbench,
    RepoType_QueryParam,
} from 'vso-workbench';

import { ApplicationState } from '../../reducers/rootReducer';
import { defaultConfig } from '../../services/configurationService';
import { credentialsProvider } from '../../providers/credentialsProvider';

type Params = { org: string; repoId: string; commitId: string };

export interface GitHubWorkbenchProps extends RouteComponentProps<Params> {
    org: string;
    repoId: string;
    commitId: string;
    filePath: string;
    selectionLineStart: string;
    selectionCharStart: string;
    selectionLineEnd: string;
    selectionCharEnd: string;
    richNavWebExtensionEndpoint: string;
}

const CODETOUR_ENDPOINT = 'https://vscsextensionsdev.blob.core.windows.net/codetour';

class GitHubWorkbenchView extends Component<GitHubWorkbenchProps, GitHubWorkbenchProps> {
    render() {
        var extensionUrls: string[] = [CODETOUR_ENDPOINT];
        var commitId = this.props.commitId ? '+' + this.props.commitId : '';
        var folderUri = `github://${this.props.org}+${this.props.repoId}${commitId}/`;

        if (localStorage.getItem('vscs-showserverless') !== 'true') {
            extensionUrls = [this.props.richNavWebExtensionEndpoint, CODETOUR_ENDPOINT];

            // Repo Info to pass to Rich Code Nav should be stored in the workspace URI
            const uriQueryObj = {
                repoType: RepoType_QueryParam.GitHub,
                org: this.props.org,
                repoId: this.props.repoId,
                commitId: this.props.commitId,
                filePath: this.props.filePath,
                selectionLineStart: this.props.selectionLineStart,
                selectionCharStart: this.props.selectionCharStart,
                selectionLineEnd: this.props.selectionLineEnd,
                selectionCharEnd: this.props.selectionCharEnd
            };
            const uriQueryString = JSON.stringify(uriQueryObj);
            folderUri = `vsck:/Rich Code Navigation/?${uriQueryString}`;
        }

        return <ServerlessWorkbench folderUri={folderUri} extensionUrls={extensionUrls} credentialsProvider={credentialsProvider} />;
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
    const selectionLineStartParam = new URLSearchParams(props.location.search).get('selectionLineStart');
    const selectionCharStartParam = new URLSearchParams(props.location.search).get('selectionCharStart');
    const selectionLineEndParam = new URLSearchParams(props.location.search).get('selectionLineEnd');
    const selectionCharEndParam = new URLSearchParams(props.location.search).get('selectionCharEnd');
    const selectionLineStart = selectionLineStartParam ? selectionLineStartParam : '';
    const selectionCharStart = selectionCharStartParam ? selectionCharStartParam : '';
    const selectionLineEnd = selectionLineEndParam ? selectionLineEndParam : '';
    const selectionCharEnd = selectionCharEndParam ? selectionCharEndParam : '';
    const { richNavWebExtensionEndpoint } = state.configuration || defaultConfig;

    return {
        org,
        repoId,
        commitId,
        filePath,
        selectionLineStart,
        selectionCharStart,
        selectionLineEnd,
        selectionCharEnd,
        richNavWebExtensionEndpoint,
    };
};

type MappedProperties = keyof ReturnType<typeof getProps>;

export const GitHubWorkbench: ConnectedComponent<
    typeof GitHubWorkbenchView,
    Omit<GitHubWorkbenchProps, MappedProperties> & RouteComponentProps<Params>
> = connect(getProps)(GitHubWorkbenchView);
