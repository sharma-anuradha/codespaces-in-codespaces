import React, { Component } from 'react';
import { connect } from 'react-redux';
import { RouteComponentProps } from 'react-router-dom';
import './workbench.css';

import { trace } from '../../utils/trace';
import { vscodeConfig } from '../../constants';

import { VSLSWebSocket, envConnector } from '../../resolvers/vslsResolver';
import { ITokenWithMsalAccount } from '../../typings/ITokenWithMsalAccount';

import { ApplicationState } from '../../reducers/rootReducer';
import { isEnvironmentAvailable, isNotAvailable } from '../../utils/environmentUtils';

import { UrlCallbackProvider } from '../../providers/urlCallbackProvider';
import { credentialsProvider } from '../../providers/credentialsProvider';
import { WorkspaceProvider } from '../../providers/workspaceProvider';
import { ExternalUriProvider } from '../../providers/externalUriProvider';
import { resourceUriProviderFactory } from '../../common/url-utils';
import { postServiceWorkerMessage } from '../../common/post-message';
import { disconnectCloudEnv } from '../../common/service-worker-messages';
import { UserDataProvider } from '../../utils/userDataProvider';

import { vscode } from '../../utils/vscode';

import { ILocalCloudEnvironment, ICloudEnvironment } from '../../interfaces/cloudenvironment';
import { IWorkbenchConstructionOptions, IWebSocketFactory, URI } from 'vscode-web';
import { telemetry } from '../../utils/telemetry';
import { defaultConfig } from '../../services/configurationService';
import { isDefined } from '../../utils/isDefined';
import { environmentsPath } from '../../routerPaths';
import { createUniqueId } from '../../dependencies';

export interface WorkbenchProps extends RouteComponentProps<{ id: string }> {
    liveShareEndpoint: string;
    token: ITokenWithMsalAccount | undefined;
    environmentInfo: ILocalCloudEnvironment | undefined;
    params: URLSearchParams;
    correlationId?: string | null;
}

const managementFavicon = 'favicon.ico';
const vscodeFavicon = 'static/web-standalone/favicon.ico';
function updateFavicon(isMounting: boolean = true) {
    const link = document.querySelector("link[rel='shortcut icon']");
    if (link) {
        const iconPath = isMounting ? vscodeFavicon : managementFavicon;
        link.setAttribute('href', iconPath);
    }
}

class WorkbenchView extends Component<WorkbenchProps> {
    // Since we have external scripts running outside of react scope,
    // we'll mange the instantiation flag outside of state as well.
    private workbenchMounted: boolean = false;

    // Not used in rendering and we change it from props by navigating
    // away so user isn't left with dangling correlationId query param.
    private correlationId?: string;

    componentDidUpdate() {
        const { environmentInfo } = this.props;
        if (!isEnvironmentAvailable(environmentInfo)) {
            if (
                isDefined(environmentInfo) &&
                isDefined(environmentInfo.state) &&
                isNotAvailable(environmentInfo)
            ) {
                this.props.history.push(environmentsPath);
            }
            return;
        }

        this.mountWorkbench(environmentInfo);
    }

    componentDidMount() {
        updateFavicon(true);

        this.correlationId = this.props.correlationId || createUniqueId();
        const searchParams = new URLSearchParams(this.props.location.search);
        searchParams.delete('correlationId');
        this.props.history.replace({
            ...location,
            search: searchParams.toString(),
        });

        const { environmentInfo } = this.props;
        if (!isEnvironmentAvailable(environmentInfo)) {
            return;
        }

        this.mountWorkbench(environmentInfo);
    }

    componentWillUnmount() {
        updateFavicon(false);
    }

    async mountWorkbench(environmentInfo: ICloudEnvironment) {
        if (this.workbenchMounted) {
            return;
        }

        await vscode.getVSCode();

        if (!this.workbenchRef) {
            return;
        }

        this.workbenchMounted = true;

        const { accessToken } = this.props.token!;

        // We start setting up the LiveShare connection here, so loading workbench assets and creating connection can go in parallel.
        envConnector.ensureConnection(environmentInfo, accessToken, this.props.liveShareEndpoint);

        const listener = () => {
            window.removeEventListener('beforeunload', listener);

            postServiceWorkerMessage({
                type: disconnectCloudEnv,
                payload: {
                    sessionId: environmentInfo.connection.sessionId,
                },
            });
        };
        window.addEventListener('beforeunload', listener);

        const resourceUriProvider = resourceUriProviderFactory(
            environmentInfo.connection.sessionId,
            envConnector
        );

        const userDataProvider = new UserDataProvider();
        await userDataProvider.initializeDBProvider();

        const correlationId = this.correlationId;
        if (!correlationId) {
            throw new Error('correlationId must be set at this point');
        }

        const { liveShareEndpoint } = this.props;
        const VSLSWebSocketFactory: IWebSocketFactory = {
            create(url: string) {
                return new VSLSWebSocket(
                    url,
                    accessToken,
                    environmentInfo,
                    liveShareEndpoint,
                    correlationId
                );
            },
        };

        const resolveCommonTelemetryProperties = () => {
            const vsoContextProperties = telemetry.getContext();
            const keys = Object.keys(vsoContextProperties) as (keyof typeof vsoContextProperties)[];
            return keys.reduce(
                (commonProperties, property) => {
                    return {
                        ...commonProperties,
                        [`vso.${property}`]: vsoContextProperties[property],
                    };
                },
                {} as { [key: string]: any }
            );
        };

        const workspaceProvider = new WorkspaceProvider(this.props.params, environmentInfo);

        const externalUriProvider = new ExternalUriProvider(
            environmentInfo,
            accessToken,
            envConnector,
            liveShareEndpoint
        );

        const resolveExternalUri = (uri: URI): Promise<URI> => {
            return externalUriProvider.resolveExternalUri(uri);
        };

        const config: IWorkbenchConstructionOptions = {
            workspaceProvider,
            remoteAuthority: `vsonline+${environmentInfo.id}`,
            webSocketFactory: VSLSWebSocketFactory,
            urlCallbackProvider: new UrlCallbackProvider(),
            connectionToken: vscodeConfig.commit,
            credentialsProvider,
            resourceUriProvider,
            userDataProvider,
            resolveExternalUri,
            resolveCommonTelemetryProperties,
        };

        trace(`Creating workbench on #${this.workbenchRef}, with config: `, config);
        vscode.create(this.workbenchRef, config);
    }

    private workbenchRef: HTMLDivElement | null = null;

    render() {
        return (
            <div className='vsonline-workbench'>
                <div
                    id='workbench'
                    style={{ height: '100%' }}
                    ref={
                        // tslint:disable-next-line: react-this-binding-issue
                        (el) => (this.workbenchRef = el)
                    }
                />
            </div>
        );
    }
}

const getProps = (state: ApplicationState, props: RouteComponentProps<{ id: string }>) => {
    const environmentInfo = state.environments.environments.find((e) => {
        return e.id === props.match.params.id;
    });

    const { liveShareEndpoint } = state.configuration || defaultConfig;

    const params = new URLSearchParams(props.location.search);

    return {
        token: state.authentication.token,
        environmentInfo,
        params,
        liveShareEndpoint,
        correlationId: params.get('correlationId'),
    };
};

export const Workbench = connect(getProps)(WorkbenchView);
