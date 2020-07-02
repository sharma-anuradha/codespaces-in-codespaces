import React, { Component, ComponentClass } from 'react';
import { connect } from 'react-redux';
import { RouteComponentProps, withRouter } from 'react-router-dom';

import { getAzDevCredentials } from '../../actions/getAzDevCredentials';
import { ApplicationState } from '../../reducers/rootReducer';
import { defaultConfig } from '../../services/configurationService';
import { Loader } from '../loader/loader';
import { PageNotFound } from '../pageNotFound/pageNotFound';
import {
    RepoType_QueryParam,
    ServerlessWorkbench,
} from '../serverlessWorkbench/serverlessWorkbench';
import { withTranslation, WithTranslation } from 'react-i18next';

type Params = {
    org: string;
    projectName: string;
    repoName: string;
    commitId: string;
};

export interface AzDevWorkbenchProps extends RouteComponentProps<Params>,  WithTranslation {
    org: string;
    projectName: string;
    repoName: string;
    commitId: string;
    filePath: string;
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
        };
        const uriQueryString = JSON.stringify(uriQueryObj);
        const folderUri = `vsck:/Rich Code Navigation/?${uriQueryString}`;

        return <ServerlessWorkbench folderUri={folderUri} extensionUrls={extensionUrls} />;
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

type MappedProperties = keyof ReturnType<typeof getProps>;
type ExternalProps = Omit<
    AzDevWorkbenchProps,
    MappedProperties | keyof RouteComponentProps<Params> | keyof WithTranslation
>;

export const AzDevWorkbench: ComponentClass<ExternalProps> = 
    withRouter(withTranslation()(connect(getProps)(AzDevWorkbenchView)));
