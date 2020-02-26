import React, { Component } from 'react';
import { connect } from 'react-redux';
import { RouteComponentProps, withRouter } from 'react-router-dom';

import { vscode } from '../../utils/vscode';
import { URI, IApplicationLink } from 'vscode-web';
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
    private applicationLinksProvider: () => IApplicationLink[];

    constructor(props: LiveShareWorkbenchProps) {
        super(props);

        const externalUriProvider = new LiveShareExternalUriProvider(props.sessionId);
        this.resolveExternalUri = (uri: URI): Promise<URI> => {
            return externalUriProvider.resolveExternalUri(uri);
        };

        this.applicationLinksProvider = () => {
            const link: IApplicationLink = {
                uri: vscode.URI.parse(
                    `vsls:?action=join&workspaceId=${this.props.sessionId}&correlationId=null`
                ),
                label: 'Open in Desktop',
            };
            return [link];
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

        const commands = [
            {
                id: '_liveshareweb.gotoSessionPage',
                handler: () =>
                    (window.location.href = `https://prod.liveshare.vsengsaas.visualstudio.com/join?${this.props.sessionId}`),
            },
        ];

        return (
            <ServerlessWorkbench
                folderUri={folderUri}
                extensionUrls={extensionUrls}
                resolveExternalUri={this.resolveExternalUri}
                applicationLinksProvider={this.applicationLinksProvider}
                commands={commands}
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
