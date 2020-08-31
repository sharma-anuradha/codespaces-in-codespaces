import React, { Component, ComponentClass } from 'react';
import { connect } from 'react-redux';
import { RouteComponentProps, withRouter } from 'react-router-dom';

import { getAzDevCredentials } from '../../actions/getAzDevCredentials';
import { ApplicationState } from '../../reducers/rootReducer';
import { defaultConfig } from '../../services/configurationService';
import { Loader } from '../loader/loader';
import {
    RepoType_QueryParam,
    ServerlessWorkbench,
} from 'vso-workbench';
import { withTranslation, WithTranslation } from 'react-i18next';
import { credentialsProvider } from '../../providers/credentialsProvider';

type Params = {
    org: string;
    projectName: string;
    repoName: string;
    commitId: string;
};

export interface AzureWorkbenchProps extends RouteComponentProps<Params>,  WithTranslation {
    org: string;
    projectName: string;
    repoName: string;
    commitId: string;
    filePath: string;
    selectionLineStart: string;
    selectionCharStart: string;
    selectionLineEnd: string;
    selectionCharEnd: string;
    richNavWebExtensionEndpoint: string;
}

export interface AzureWorkbenchState {
    loading: boolean;
}

class AzureWorkbenchView extends Component<AzureWorkbenchProps, AzureWorkbenchState> {
    constructor(props: AzureWorkbenchProps) {
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
        const { t: translation } = this.props;

        if (this.state.loading) {
            return <Loader message={translation('loggingAzureDevOps')} translation={translation}></Loader>;
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
            selectionLineStart: this.props.selectionLineStart,
            selectionCharStart: this.props.selectionCharStart,
            selectionLineEnd: this.props.selectionLineEnd,
            selectionCharEnd: this.props.selectionCharEnd
        };
        const uriQueryString = JSON.stringify(uriQueryObj);
        const folderUri = `vsck:/Rich Code Navigation/?${uriQueryString}`;

        return <ServerlessWorkbench folderUri={folderUri} extensionUrls={extensionUrls} credentialsProvider={credentialsProvider} />;
    }
}

const getProps = (state: ApplicationState, props: { match: { params: Params } 
                                                    location : { search : string }}) => {
    const org = props.match.params.org;
    const projectName = props.match.params.projectName;
    const repoName = props.match.params.repoName;
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
        projectName,
        repoName,
        filePath,
        commitId,
        selectionLineStart,
        selectionCharStart,
        selectionLineEnd,
        selectionCharEnd,
        richNavWebExtensionEndpoint,
    };
};

type MappedProperties = keyof ReturnType<typeof getProps>;
type ExternalProps = Omit<
    AzureWorkbenchProps,
    MappedProperties | keyof RouteComponentProps<Params> | keyof WithTranslation
>;

export const AzureWorkbench: ComponentClass<ExternalProps> = 
    withRouter(withTranslation()(connect(getProps)(AzureWorkbenchView)));
