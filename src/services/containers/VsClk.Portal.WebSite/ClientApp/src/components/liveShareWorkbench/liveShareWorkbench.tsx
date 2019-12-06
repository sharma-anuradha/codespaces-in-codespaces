import React, { Component } from 'react';
import { connect } from 'react-redux';
import { RouteComponentProps } from 'react-router-dom';

import { URI } from 'vscode-web';
import { LiveShareExternalUriProvider } from '../../providers/externalUriProvider';

import { ApplicationState } from '../../reducers/rootReducer';
import { ServerlessWorkbench } from '../serverlessWorkbench/serverlessWorkbench';

export interface LiveShareWorkbenchProps extends RouteComponentProps<{ id: string }> {
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
        // Get the metadata for the LiveShare static extension
        const packageJSON: any = require('@vsliveshare/liveshare-web/liveshare/package.json');
        packageJSON.extensionKind = ['web']; // enable for Web
        const extensionLocation = `https://${window.location.hostname}/static/web-standalone/staticExtensions/liveshare/`;
        const staticExtensions = [
            {
                packageJSON,
                extensionLocation,
            },
        ];

        // This is the folder URI format recognized by the LiveShare file system provider.
        const folderUri = `vsls:///?${this.props.sessionId}`;

        return (
            <ServerlessWorkbench
                folderUri={folderUri}
                staticExtensions={staticExtensions}
                resolveExternalUri={this.resolveExternalUri}
            />
        );
    }
}

const getProps = (state: ApplicationState, props: RouteComponentProps<{ id: string }>) => {
    const sessionId = props.match.params.id;

    return {
        sessionId,
    };
};

export const LiveShareWorkbench = connect(getProps)(LiveShareWorkbenchView);
