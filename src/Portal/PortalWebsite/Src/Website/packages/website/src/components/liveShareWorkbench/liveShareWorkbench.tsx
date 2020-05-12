import React, { Component, ComponentClass } from 'react';
import { connect } from 'react-redux';
import { RouteComponentProps, withRouter } from 'react-router-dom';

import { URI, IApplicationLink, IHostCommand } from 'vscode-web';

import { ApplicationState } from '../../reducers/rootReducer';
import { ServerlessWorkbench } from '../serverlessWorkbench/serverlessWorkbench';
import { defaultConfig } from '../../services/configurationService';

import { getShortEnvironmentName, isDevEnvironment } from '../../utils/getShortEnvironmentName';
import { telemetry } from '../../utils/telemetry';
import { vscode, PortForwardingExternalUriProvider } from 'vso-workbench';
import { LiveShareExternalUriProvider } from '../../providers/externalUriProvider';
import { createTrace } from 'vso-client-core';

declare var AMDLoader: any;
let CallingService: any;

export interface LiveShareWorkbenchProps extends RouteComponentProps<{ id: string }> {
    liveShareWebExtensionEndpoint: string;
    portalEndpoint: string;
    portForwardingDomainTemplate: string;
    sessionId: string;
    enableEnvironmentPortForwarding: boolean;
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

class LiveShareWorkbenchView extends Component<LiveShareWorkbenchProps> {
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

        if (props.enableEnvironmentPortForwarding) {
            const externalUriProvider = new PortForwardingExternalUriProvider(
                props.portForwardingDomainTemplate,
                props.sessionId
            );
            this.resolveExternalUri = externalUriProvider.resolveExternalUri;
        } else {
            const externalUriProvider = new LiveShareExternalUriProvider(props.sessionId);
            this.resolveExternalUri = (uri: URI): Promise<URI> => {
                return externalUriProvider.resolveExternalUri(uri);
            };
        }

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
        let { liveShareWebExtensionEndpoint: extensionUrl,
            portalEndpoint: env } = this.props;

        // In the dev environment allow a localhost url to make it easy to test
        // LiveShare changes locally
        if (isDevEnvironment(env)) {
            extensionUrl = 'http://localhost:5500/web/deploy-web';
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

        return (
            <ServerlessWorkbench
                folderUri={folderUri}
                extensionUrls={extensionUrls}
                resolveExternalUri={this.resolveExternalUri}
                applicationLinksProvider={this.applicationLinksProvider}
                resolveCommands={getResolveCommands(extensionUrl, this.props.sessionId)}
            />
        );
    }
}

const getProps = (state: ApplicationState, props: { match: { params: { id: string } } }) => {
    const sessionId = props.match.params.id;

    const {
        liveShareWebExtensionEndpoint,
        portalEndpoint,
        portForwardingDomainTemplate,
        enableEnvironmentPortForwarding,
    } = state.configuration || defaultConfig;

    return {
        sessionId,
        portalEndpoint,
        liveShareWebExtensionEndpoint,
        portForwardingDomainTemplate,
        enableEnvironmentPortForwarding,
    };
};

const getCallingServiceApi = async (callingServiceUrl: string): Promise<any> => {
    return new Promise((resolve) => {
        AMDLoader.global.require(
            [callingServiceUrl],
            (calling: any) => {
                resolve(calling);
            }
        );
    });
}

const getResolveCommands = (extensionUrl: string, sessionId: string) => {
    return async (): Promise<IHostCommand[]> => {
        const trace = createTrace('CallingService');
        const callingApi = await getCallingServiceApi(`${extensionUrl}/out/callingService.js`);
        CallingService = new callingApi.CallingService(vscode, trace);
        return [
            {
                id: '_liveshareweb.gotoSessionPage',
                handler: () =>
                    (window.location.href = `https://prod.liveshare.vsengsaas.visualstudio.com/join?${sessionId}`),
            },
            {
                id: '_liveshareweb.getMachineId',
                handler: () => telemetry.getContext().browserId,
            },
            ...CallingService.getCommands()
        ];
    }
};

type ExternalProps = Omit<
    LiveShareWorkbenchProps,
    keyof ReturnType<typeof getProps> | keyof RouteComponentProps
>;

export const LiveShareWorkbench: ComponentClass<ExternalProps> = withRouter(
    connect(getProps)(LiveShareWorkbenchView)
);
