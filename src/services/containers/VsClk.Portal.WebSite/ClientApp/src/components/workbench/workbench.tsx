import React, { Component } from 'react';
import { connect } from 'react-redux';
import { RouteComponentProps } from 'react-router-dom';

import {
    IWorkbenchConstructionOptions,
    IWebSocketFactory,
    IHomeIndicator,
    URI
} from 'vscode-web';

import {
    createTrace,
    ILocalEnvironment,
    IEnvironment,
    isHostedOnGithub,
    vsls,
} from 'vso-client-core';

import {
    disconnectCloudEnv,
    postServiceWorkerMessage,
    EnvConnector,
    VSLSWebSocket,
} from 'vso-ts-agent';

import {
    UserDataProvider,
    UrlCallbackProvider,
    WorkspaceProvider,
    resourceUriProviderFactory,
    applicationLinksProviderFactory,
    vscode,
    getVSCodeVersion,
    getExtensions,
    DEFAULT_GITHUB_VSCODE_AUTH_PROVIDER_ID,
} from 'vso-workbench';

import { getWorkbenchDefaultLayout } from './getWorkbenchDefaultLayout';

import * as envRegService from '../../services/envRegService';

import { BrowserSyncService } from '../../rpcServices/BrowserSyncService';
import { GitCredentialService } from '../../rpcServices/GitCredentialService';

import { ApplicationState } from '../../reducers/rootReducer';
import {
    isEnvironmentAvailable,
    isActivating,
    isSuspended,
    isNotAvailable,
    isStarting,
} from '../../utils/environmentUtils';

import { credentialsProvider } from '../../providers/credentialsProvider';

const getWorkspaceUrl = (defaultUrl: URL) => {
    if (!isHostedOnGithub()) {
        return defaultUrl;
    }

    const result = PostMessageRepoInfoRetriever.getStoredInfo();
    if (!result) {
        throw new Error('No environmentId info found.');
    }

    const url = new URL(
        `https://github.com/workspaces/${result.ownerUsername}/${result.workspaceId}`
    );

    return url;
};

import { telemetry } from '../../utils/telemetry';
import { updateFavicon } from '../../utils/updateFavicon';
import { defaultConfig } from '../../services/configurationService';
import { createUniqueId } from '../../dependencies';
import { connectEnvironment } from '../../actions/connectEnvironment';
import { pollActivatingEnvironment } from '../../actions/pollEnvironment';
import { useActionContext } from '../../actions/middleware/useActionContext';
import { CommunicationAdapter } from '../../services/communicationAdapter';
import { SplashCommunicationProvider } from '../../providers/splashCommunicationProvider';
import { IWorkbenchSplashScreenProps } from '../../interfaces/IWorkbenchSplashScreenProps';
import { Loader } from '../loader/loader';

import { PostMessageRepoInfoRetriever } from '../../split/github/postMessageRepoInfoRetriever';
import {
    EnvironmentsExternalUriProvider,
    PortForwardingExternalUriProvider,
} from '../../providers/externalUriProvider';

import './workbench.css';

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
    apiEndpoint: string;
    portForwardingDomainTemplate: string;
    token: string | undefined;
    environmentInfo: ILocalEnvironment | undefined;
    getEnvironment: typeof envRegService.getEnvironment;
    params: URLSearchParams;
    correlationId?: string | null;
    isValidEnvironmentFound: boolean;
    connectEnvironment: (
        ...params: Parameters<typeof connectEnvironment>
    ) => ReturnType<typeof connectEnvironment>;
    pollEnvironment: (
        ...params: Parameters<typeof pollActivatingEnvironment>
    ) => ReturnType<typeof pollActivatingEnvironment>;
    enableEnvironmentPortForwarding: boolean;
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

    // Seconds for timeout when starting
    private notifySeconds?: number;
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
    checkForEnvironmentStatus(environmentInfo: ILocalEnvironment | undefined) {
        if (!environmentInfo) {
            return;
        }

        if (isEnvironmentAvailable(environmentInfo)) {
            if (this.communicationProvider && environmentInfo.id) {
                this.communicationProvider.postEnvironmentId(environmentInfo.id);
            }

            this.cancelPolling();
            this.mountWorkbench(environmentInfo as IEnvironment);

            return;
        }

        if (this.state && this.state.connectError) {
            return;
        }

        if (this.notifySeconds && Date.now() >= this.notifySeconds) {
            this.notifySeconds = undefined;
            this.communicationProvider?.sendNotification(
                'Looks like this is taking a little longer than usual but your environment will be ready soon'
            );
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
                    if (isStarting(environmentInfo)) {
                        this.communicationProvider?.appendSteps([
                            {
                                name: 'Resume Environment',
                                data: {
                                    status: 'Pending',
                                    terminal: 'false',
                                },
                            },
                        ]);
                        //Notify after 30 seconds
                        this.notifySeconds = Date.now() + 30 * 1000;
                    } else {
                        communicationAdapter.connect(environmentInfo.connection.sessionId);
                    }
                } catch (e) {
                    logger.info(`Connection failed ${e}`);
                }
            }
        }

        if (!this.props.autoStart) {
            return;
        }

        if (!this.isConnecting && isSuspended(environmentInfo) && environmentInfo.id) {
            this.isConnecting = true;
            this.props
                .connectEnvironment(environmentInfo.id, environmentInfo.state)
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

    pollForActivatingEnvironment(environmentInfo: ILocalEnvironment) {
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
    async mountWorkbench(environmentInfo: IEnvironment) {
        const { token, liveShareEndpoint, apiEndpoint, getEnvironment } = this.props;

        if (this.workbenchMounted) {
            return;
        } else {
            this.workbenchMounted = true;
        }

        await vscode.getVSCode();

        if (!this.workbenchRef) {
            return;
        }

        if (!token) {
            throw new Error('No access token present.');
        }

        const envConnector = new EnvConnector(async (e) => {
            const { workspaceClient, workspaceService, rpcConnection } = e;

            // Expose credential service
            const gitCredentialService = new GitCredentialService(workspaceService, rpcConnection);
            await gitCredentialService.shareService();

            // Expose browser sync service
            const sourceEventService = workspaceClient.getServiceProxy<vsls.SourceEventService>(
                vsls.SourceEventService
            );

            new BrowserSyncService(sourceEventService);
        });

        // We start setting up the LiveShare connection here, so loading workbench assets and creating connection can go in parallel.
        envConnector.ensureConnection(
            environmentInfo,
            token,
            liveShareEndpoint,
            getVSCodeVersion(),
            getExtensions(),
            apiEndpoint
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

        const vscodeVersion = getVSCodeVersion();
        const resourceUriProvider = resourceUriProviderFactory(
            vscodeVersion.commit,
            environmentInfo.connection.sessionId,
            envConnector
        );

        const defaultSettings = isHostedOnGithub()
            ? '{"workbench.colorTheme": "GitHub Light"}'
            : '';

        if (!isHostedOnGithub()) {
            localStorage.setItem('vscode.baseTheme', 'vs-dark');
        }

        const userDataProvider = new UserDataProvider(defaultSettings);
        await userDataProvider.initializeDBProvider();

        const correlationId = this.correlationId;
        if (!correlationId) {
            throw new Error('correlationId must be set at this point');
        }

        const VSLSWebSocketFactory: IWebSocketFactory = {
            create(url: string) {
                return new VSLSWebSocket(
                    url,
                    token,
                    liveShareEndpoint,
                    correlationId,
                    getVSCodeVersion(),
                    envConnector,
                    logger.verbose,
                    getEnvironment,
                    getExtensions(),
                    apiEndpoint
                );
            },
        };

        const resolveCommonTelemetryProperties = telemetry.resolveCommonProperties.bind(telemetry);

        const workspaceProvider = new WorkspaceProvider(
            this.props.params,
            environmentInfo,
            getWorkspaceUrl
        );
        const applicationLinks = applicationLinksProviderFactory(workspaceProvider);

        let resolveExternalUri;
        if (this.props.enableEnvironmentPortForwarding) {
            const ensurePortIsForwarded = envConnector.ensurePortIsForwarded.bind(
                envConnector,
                environmentInfo,
                token,
                liveShareEndpoint
            );
            const externalUriProvider = new PortForwardingExternalUriProvider(
                this.props.portForwardingDomainTemplate,
                environmentInfo.id,
                ensurePortIsForwarded
            );
            resolveExternalUri = externalUriProvider.resolveExternalUri;
        } else {
            const externalUriProvider = new EnvironmentsExternalUriProvider(
                environmentInfo,
                token,
                envConnector,
                liveShareEndpoint
            );
            resolveExternalUri = (uri: URI): Promise<URI> => {
                return externalUriProvider.resolveExternalUri(uri);
            };
        }

        const homeIndicator: IHomeIndicator | undefined = isHostedOnGithub()
            ? {
                  command: '_github.gohome',
                  icon: 'github-inverted',
                  title: 'Go Home',
              }
            : undefined;

        const commands = [
            {
                id: '_github.gohome',
                handler: () => {
                    PostMessageRepoInfoRetriever.sendGoHomeMessage();
                },
            },
        ];

        const defaultLayout = getWorkbenchDefaultLayout(
            environmentInfo,
            userDataProvider.isFirstRun
        );

        const authenticationSessionId = isHostedOnGithub()
            ? DEFAULT_GITHUB_VSCODE_AUTH_PROVIDER_ID
            : undefined;

        const config: IWorkbenchConstructionOptions = {
            remoteAuthority: `vsonline+${environmentInfo.id}`,
            webSocketFactory: VSLSWebSocketFactory,
            connectionToken: vscodeVersion.commit,
            workspaceProvider,
            credentialsProvider,
            userDataProvider,
            urlCallbackProvider: new UrlCallbackProvider(),
            resourceUriProvider,
            resolveExternalUri,
            resolveCommonTelemetryProperties,
            applicationLinks,
            homeIndicator,
            commands,
            authenticationSessionId,
            enableSyncByDefault: false,
            defaultLayout,
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

        if (!isNotAvailable(environmentInfo)) {
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

        return <div>{content}</div>;
    }
}

const getProps = (state: ApplicationState, props: RouteComponentProps<{ id: string }>) => {
    const environmentInfo = state.environments.environments.find((e) => {
        return e.id === props.match.params.id;
    });

    const {
        liveShareEndpoint,
        apiEndpoint,
        portForwardingDomainTemplate,
        enableEnvironmentPortForwarding,
    } = state.configuration || defaultConfig;

    const params = new URLSearchParams(props.location.search);

    const isValidEnvironmentFound =
        !environmentInfo && state.environments.isLoading === false ? false : true;

    return {
        ...props,
        token: state.authentication.token,
        environmentInfo,
        params,
        liveShareEndpoint,
        getEnvironment: envRegService.getEnvironment,
        apiEndpoint,
        portForwardingDomainTemplate,
        enableEnvironmentPortForwarding,
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
