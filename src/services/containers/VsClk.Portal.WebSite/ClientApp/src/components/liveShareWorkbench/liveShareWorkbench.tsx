import React, { Component } from 'react';
import { connect } from 'react-redux';
import { RouteComponentProps, withRouter } from 'react-router-dom';

import { vscode } from '../../utils/vscode';
import { URI, IApplicationLink } from 'vscode-web';
import { LiveShareExternalUriProvider } from '../../providers/externalUriProvider';

import { ApplicationState } from '../../reducers/rootReducer';
import { ServerlessWorkbench } from '../serverlessWorkbench/serverlessWorkbench';
import { defaultConfig } from '../../services/configurationService';

import { getShortEnvironmentName } from '../../utils/getShortEnvironmentName';
import { telemetry } from '../../utils/telemetry';

export interface LiveShareWorkbenchProps extends RouteComponentProps<{ id: string }> {
    liveShareWebExtensionEndpoint: string;
    portalEndpoint: string;
    sessionId: string;
}

const liveShareEnvParam = (env: string, currentSearch: string): string | null => {
    const currentEnv = getShortEnvironmentName(env);

    const isDEV = currentEnv === 'dev';
    const isDEVStg = currentEnv === 'dev-stg';
    const isPPE = currentEnv === 'ppe';
    if (!isDEV && !isDEVStg && !isPPE) {
        return null;
    }

    const params = new URLSearchParams(currentSearch);
    /// if param already set, don't override it
    const setParam = params.get('env');
    if (setParam && setParam.trim()) {
        return setParam;
    }

    return currentEnv === 'dev-stg' ? 'dev' : currentEnv;
};

class LiveShareWorkbenchView extends Component<LiveShareWorkbenchProps, LiveShareWorkbenchProps> {
    private resolveExternalUri: (uri: URI) => Promise<URI>;
    private applicationLinksProvider: () => IApplicationLink[];

    private getSessionLinkParamsWithEnvironment = (otherParams: string[][]) => {
        const { history, portalEndpoint } = this.props;
        const { location } = history;

        const params = new URLSearchParams(otherParams);

        const envParam = liveShareEnvParam(portalEndpoint, location.search);
        if (envParam) {
            params.append('env', envParam);
        }

        return params;
    };

    constructor(props: LiveShareWorkbenchProps) {
        super(props);

        const externalUriProvider = new LiveShareExternalUriProvider(props.sessionId);
        this.resolveExternalUri = (uri: URI): Promise<URI> => {
            return externalUriProvider.resolveExternalUri(uri);
        };

        this.applicationLinksProvider = () => {
            const params = this.getSessionLinkParamsWithEnvironment([
                ['action', 'join'],
                ['workspaceId', this.props.sessionId],
                ['correlationId', 'null'],
            ]);

            const link: IApplicationLink = {
                uri: vscode.URI.parse(`vsls:?${params}`),
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

        const { sessionId } = this.props;
        const params = this.getSessionLinkParamsWithEnvironment([
            ['sessionId', this.props.sessionId],
        ]);

        // This is the folder URI format recognized by the LiveShare file system provider.
        // we use `?{sessionId}` for cbackward compat reasons, should be removed
        // when LS extension understands the `sessionId={id}` parameter
        const folderUri = `vsls:///?${sessionId}&${params}`;

        const commands = [
            {
                id: '_liveshareweb.gotoSessionPage',
                handler: () =>
                    (window.location.href = `https://prod.liveshare.vsengsaas.visualstudio.com/join?${this.props.sessionId}`),
            },
            {
                id: '_liveshareweb.getMachineId',
                handler: () => telemetry.getContext().browserId,
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

    const { liveShareWebExtensionEndpoint, portalEndpoint } = state.configuration || defaultConfig;
    return {
        sessionId,
        portalEndpoint,
        liveShareWebExtensionEndpoint,
    };
};

export const LiveShareWorkbench = withRouter(connect(getProps)(LiveShareWorkbenchView));
