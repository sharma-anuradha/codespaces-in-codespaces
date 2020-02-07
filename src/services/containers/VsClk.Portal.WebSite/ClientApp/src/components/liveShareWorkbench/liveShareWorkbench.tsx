import React, { Component } from 'react';
import { connect } from 'react-redux';
import { RouteComponentProps, withRouter } from 'react-router-dom';

import { URI } from 'vscode-web';
import { LiveShareExternalUriProvider } from '../../providers/externalUriProvider';

import { ApplicationState } from '../../reducers/rootReducer';
import { ServerlessWorkbench } from '../serverlessWorkbench/serverlessWorkbench';
import { defaultConfig } from '../../services/configurationService';

export interface LiveShareWorkbenchProps extends RouteComponentProps<{ id: string }> {
    liveShareWebExtensionEndpoint: string;
    sessionId: string;
}

class LiveShareWorkbenchView extends Component<LiveShareWorkbenchProps, LiveShareWorkbenchProps> {
    private resolveExternalUri: (uri: URI) => Promise<URI>;

    constructor(props: LiveShareWorkbenchProps) {
        super(props);

        const externalUriProvider = new LiveShareExternalUriProvider(props.sessionId);
        this.resolveExternalUri = (uri: URI): Promise<URI> => {
            return externalUriProvider.resolveExternalUri(uri);
        };
    }

    render() {
        let extensionUrl = this.props.liveShareWebExtensionEndpoint;

        // In the dev environment allow a localhost url to make it easy to test
        // LiveShare changes locally
        if (
            window.location.hostname === 'online.dev.core.vsengsaas.visualstudio.com' &&
            window.localStorage.getItem('debugLocalExtension')
        ) {
            extensionUrl = `http://localhost:5500/web/deploy-web`;
        }

        const extensionUrls = [extensionUrl];

        // This is the folder URI format recognized by the LiveShare file system provider.
        const folderUri = `vsls:///?${this.props.sessionId}`;

        return (
            <ServerlessWorkbench
                folderUri={folderUri}
                extensionUrls={extensionUrls}
                resolveExternalUri={this.resolveExternalUri}
            />
        );
    }
}

const getProps = (state: ApplicationState, props: RouteComponentProps<{ id: string }>) => {
    const sessionId = props.match.params.id;

    const { liveShareWebExtensionEndpoint } = state.configuration || defaultConfig;
    return {
        sessionId,
        liveShareWebExtensionEndpoint,
    };
};

export const LiveShareWorkbench = withRouter(connect(getProps)(LiveShareWorkbenchView));
