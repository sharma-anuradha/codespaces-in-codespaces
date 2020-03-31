import React, { Component } from 'react';
import { connect } from 'react-redux';
import { RouteComponentProps } from 'react-router-dom';
import {
    IWorkbenchConstructionOptions,
    IWebSocketFactory,
    URI,
    IApplicationLink,
} from 'vscode-web';

import { createTrace } from 'vso-client-core';

import { VSLSWebSocket, envConnector } from '../../resolvers/vslsResolver';

import { ApplicationState } from '../../reducers/rootReducer';
import {
    isEnvironmentAvailable,
    isActivating,
    isSuspended,
    isNotAvailable,
} from '../../utils/environmentUtils';

import { UrlCallbackProvider } from '../../providers/urlCallbackProvider';
import { credentialsProvider } from '../../providers/credentialsProvider';
import { WorkspaceProvider } from '../../providers/workspaceProvider';
import { EnvironmentsExternalUriProvider } from '../../providers/externalUriProvider';
import { resourceUriProviderFactory } from '../../providers/resourceUriProviderFactory';
import { postServiceWorkerMessage } from '../../common/post-message';
import { disconnectCloudEnv } from '../../common/service-worker-messages';
import { UserDataProvider } from '../../utils/userDataProvider';

import { vscode } from '../../utils/vscode';

import { ILocalCloudEnvironment, ICloudEnvironment } from '../../interfaces/cloudenvironment';
import { telemetry } from '../../utils/telemetry';
import { updateFavicon } from '../../utils/updateFavicon';
import { defaultConfig } from '../../services/configurationService';
import { createUniqueId } from '../../dependencies';
import { connectEnvironment } from '../../actions/connectEnvironment';
import { pollActivatingEnvironment } from '../../actions/pollEnvironment';
import { useActionContext } from '../../actions/middleware/useActionContext';
import './workbench.css';
import { CommunicationAdapter } from '../../services/communicationAdapter';
import { SplashCommunicationProvider } from '../../providers/splashCommunicationProvider';
import { IWorkbenchSplashScreenProps } from '../../interfaces/IWorkbenchSplashScreenProps';
import { Loader } from '../loader/loader';
import { getVscodeQuality, getVSCodeVersion } from '../../utils/featureSet';

export interface IWorkbenchState {
    connectError: string | null;
    connectRequested: boolean;
}

export interface WorkbenchProps {
    connectingFavicon: string;
    workbenchFavicon: string;
    autoStart: boolean;
    SplashScreenComponent: React.JSXElementConstructor<IWorkbenchSplashScreenProps>;
    PageNotFoundComponent: React.JSXElementConstructor<{}>;
    liveShareEndpoint: string;
    token: string | undefined;
    environmentInfo: ILocalCloudEnvironment | undefined;
    params: URLSearchParams;
    correlationId?: string | null;
    isValidEnvironmentFound: boolean;
    connectEnvironment: (
        ...params: Parameters<typeof connectEnvironment>
    ) => ReturnType<typeof connectEnvironment>;
    pollEnvironment: (
        ...params: Parameters<typeof pollActivatingEnvironment>
    ) => ReturnType<typeof pollActivatingEnvironment>;
}

const logger = createTrace('WorkbenchView');

class WorkbenchView extends Component<WorkbenchProps, IWorkbenchState> {
    constructor(props: WorkbenchProps, state: IWorkbenchState) {
        super(props, state);
        this.state = {
            connectError: null,
            connectRequested: false,
        };
    }

    // Communication provider for creation splash screen
    private communicationProvider?: SplashCommunicationProvider;
    // We need to stablish a liveshare connection until the environment
    // info is available.
    private hasConnectionStarted: boolean = false;
    // Since we have external scripts running outside of react scope,
    // we'll mange the instantiation flag outside of state as well.
    private workbenchMounted: boolean = false;

    // Not used in rendering and we change it from props by navigating
    // away so user isn't left with dangling correlationId query param.
    private correlationId?: string;

    private interval: ReturnType<typeof setInterval> | undefined;

    // When the environment is being started from suspended state,
    // and connecting to environment fails, the connectEnvironment action
    // fires a stateChange event to reset UI state from Starting to Suspended.
    // We do not want this component to try connecting to environment again
    // in this case. So we handle that here to keep track of the async operation.
    private isConnecting: boolean = false;

    componentDidUpdate() {
        const { environmentInfo } = this.props;
        this.checkForEnvironmentStatus(environmentInfo);
    }

    componentDidMount() {
        const { workbenchFavicon } = this.props;

        updateFavicon(workbenchFavicon);
        if (!this.correlationId) {
            this.correlationId = createUniqueId();
        }

        const { environmentInfo } = this.props;
        this.checkForEnvironmentStatus(environmentInfo);
    }

    onCommandReceived = (command: any) => {
        logger.info('Command received', command);

        if (command.data.command) {
            switch (command.data.command) {
                case 'connect':
                    this.setState({ connectRequested: true });
                    break;
            }
        }
    };

    // tslint:disable-next-line: max-func-body-length
    checkForEnvironmentStatus(environmentInfo: ILocalCloudEnvironment | undefined) {
        if (!environmentInfo) {
            return;
        }

        if (isEnvironmentAvailable(environmentInfo)) {
            if (this.communicationProvider) {
                this.communicationProvider.postEnvironmentId(environmentInfo.id);
            }

            this.cancelPolling();
            this.mountWorkbench(environmentInfo);

            return;
        }

        if (this.state && this.state.connectError) {
            return;
        }

        if (!this.hasConnectionStarted && this.communicationProvider) {
            this.hasConnectionStarted = true;
            const communicationAdapter = new CommunicationAdapter(
                this.communicationProvider,
                this.props.liveShareEndpoint,
                this.correlationId || createUniqueId()
            );
            if (environmentInfo.connection) {
                try {   
                    communicationAdapter.connect(environmentInfo.connection.sessionId);
                } catch (e) {
                    logger.info(`Connection failed ${e}`);
                }
            }
        }

        if (!this.props.autoStart) {
            return;
        }

        if (!this.isConnecting && isSuspended(environmentInfo)) {
            this.isConnecting = true;
            this.props
                .connectEnvironment(environmentInfo.id!, environmentInfo.state)
                .catch((error) => {
                    this.setState({ connectError: error });
                })
                .finally(() => {
                    this.isConnecting = false;
                });
            const actionContext = useActionContext();
            this.correlationId = actionContext.__id;
        } else if (isActivating(environmentInfo)) {
            this.pollForActivatingEnvironment(environmentInfo);
        }
    }

    pollForActivatingEnvironment(environmentInfo: ILocalCloudEnvironment) {
        if (isActivating(environmentInfo)) {
            if (this.interval) {
                return;
            }
            this.interval = setInterval(() => {
                this.props.pollEnvironment(environmentInfo.id!);
            }, 2000);
        }
    }

    componentWillUnmount() {
        const { connectingFavicon } = this.props;

        updateFavicon(connectingFavicon);
        this.cancelPolling();
    }

    cancelPolling() {
        if (this.interval) {
            clearInterval(this.interval);
        }
    }

    handleClickToRetry = () => {
        this.setState({ connectError: null });
    };

    handleOnSplashScreenConnect = () => {
        const url = new URL(window.location.href);
        url.searchParams.delete('autoStart');

        window.location.replace(url.toString());
    };

    // tslint:disable-next-line: max-func-body-length
    async mountWorkbench(environmentInfo: ICloudEnvironment) {
        if (this.workbenchMounted) {
            return;
        } else {
            this.workbenchMounted = true;
        }

        await vscode.getVSCode();

        if (!this.workbenchRef) {
            return;
        }

        if (!this.props.token) {
            throw new Error('No access token present.');
        }

        const quality = getVscodeQuality();

        // We start setting up the LiveShare connection here, so loading workbench assets and creating connection can go in parallel.
        envConnector.ensureConnection(
            environmentInfo,
            this.props.token,
            this.props.liveShareEndpoint,
            quality
        );

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

        const vscodeVersion = getVSCodeVersion(quality);

        const resourceUriProvider = resourceUriProviderFactory(
            vscodeVersion.commit,
            environmentInfo.connection.sessionId,
            envConnector
        );

        const userDataProvider = new UserDataProvider();
        await userDataProvider.initializeDBProvider();

        const correlationId = this.correlationId;
        if (!correlationId) {
            throw new Error('correlationId must be set at this point');
        }

        const { liveShareEndpoint, token } = this.props;
        const VSLSWebSocketFactory: IWebSocketFactory = {
            create(url: string) {
                return new VSLSWebSocket(
                    url,
                    token,
                    environmentInfo,
                    liveShareEndpoint,
                    correlationId,
                    quality
                );
            },
        };

        const resolveCommonTelemetryProperties = telemetry.resolveCommonProperties.bind(telemetry);

        const workspaceProvider = new WorkspaceProvider(this.props.params, environmentInfo);

        const externalUriProvider = new EnvironmentsExternalUriProvider(
            environmentInfo,
            token,
            envConnector,
            liveShareEndpoint
        );

        const resolveExternalUri = (uri: URI): Promise<URI> => {
            return externalUriProvider.resolveExternalUri(uri);
        };

        const link: IApplicationLink = {
            uri: workspaceProvider.getApplicationUri(quality),
            label: 'Open in Desktop',
        };

        const applicationLinks = [link];

        const config: IWorkbenchConstructionOptions = {
            workspaceProvider,
            remoteAuthority: `vsonline+${environmentInfo.id}`,
            webSocketFactory: VSLSWebSocketFactory,
            urlCallbackProvider: new UrlCallbackProvider(),
            connectionToken: vscodeVersion.commit,
            credentialsProvider,
            resourceUriProvider,
            userDataProvider,
            resolveExternalUri,
            resolveCommonTelemetryProperties,
            applicationLinks,
        };

        logger.info(`Creating workbench on #${this.workbenchRef}, with config: `, config);
        vscode.create(this.workbenchRef, config);
    }

    private workbenchRef: HTMLDivElement | null = null;

    private renderWorkbench() {
        const { environmentInfo, SplashScreenComponent } = this.props;

        if (!environmentInfo) {
            return <Loader></Loader>;
        }
        if (!isNotAvailable(environmentInfo!)) {
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
        } else {
            this.communicationProvider =
                this.communicationProvider ||
                new SplashCommunicationProvider(this.onCommandReceived);
            return (
                <SplashScreenComponent
                    onRetry={this.handleClickToRetry}
                    onConnect={this.handleOnSplashScreenConnect}
                    environment={environmentInfo}
                    showPrompt={!this.props.autoStart}
                    connectError={this.state.connectError}
                />
            );
        }
    }

    render() {
        const content = this.renderWorkbench();

        return <div className='connect-to-environment'>{content}</div>;
    }
}

const getProps = (state: ApplicationState, props: RouteComponentProps<{ id: string }>) => {
    const environmentInfo = state.environments.environments.find((e) => {
        return e.id === props.match.params.id;
    });

    const { liveShareEndpoint } = state.configuration || defaultConfig;

    const params = new URLSearchParams(props.location.search);

    const isValidEnvironmentFound =
        !environmentInfo && state.environments.isLoading === false ? false : true;

    return {
        ...props,
        token: state.authentication.token,
        environmentInfo,
        params,
        liveShareEndpoint,
        correlationId: params.get('correlationId'),
        autoStart: params.get('autoStart') !== 'false',
        isValidEnvironmentFound,
    };
};

const mapDispatch = {
    connectEnvironment,
    pollEnvironment: pollActivatingEnvironment,
};

export const Workbench = connect(getProps, mapDispatch)(WorkbenchView);
