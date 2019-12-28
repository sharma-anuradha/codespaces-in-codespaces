import React, { Component } from 'react';
import { connect } from 'react-redux';
import { RouteComponentProps } from 'react-router-dom';
import './workbench.css';

import { trace } from '../../utils/trace';
import { getVSCodeVersion } from '../../constants';

import { VSLSWebSocket, envConnector } from '../../resolvers/vslsResolver';
import { ITokenWithMsalAccount } from '../../typings/ITokenWithMsalAccount';

import { ApplicationState } from '../../reducers/rootReducer';
import {
    isEnvironmentAvailable,
    isNotAvailable,
    isActivating,
    isSuspended,
    stateToDisplayName,
} from '../../utils/environmentUtils';

import { UrlCallbackProvider } from '../../providers/urlCallbackProvider';
import { credentialsProvider } from '../../providers/credentialsProvider';
import { WorkspaceProvider } from '../../providers/workspaceProvider';
import { EnvironmentsExternalUriProvider } from '../../providers/externalUriProvider';
import { resourceUriProviderFactory } from '../../common/url-utils';
import { postServiceWorkerMessage } from '../../common/post-message';
import { disconnectCloudEnv } from '../../common/service-worker-messages';
import { UserDataProvider } from '../../utils/userDataProvider';

import { vscode } from '../../utils/vscode';

import { ILocalCloudEnvironment, ICloudEnvironment } from '../../interfaces/cloudenvironment';
import { IWorkbenchConstructionOptions, IWebSocketFactory, URI } from 'vscode-web';
import { telemetry } from '../../utils/telemetry';
import { defaultConfig } from '../../services/configurationService';
import { createUniqueId } from '../../dependencies';
import { PageNotFound } from '../pageNotFound/pageNotFound';
import { PortalLayout } from '../portalLayout/portalLayout';
import { Stack, PrimaryButton } from 'office-ui-fabric-react';
import { Loader } from '../loader/loader';
import { Text } from 'office-ui-fabric-react/lib/Text';
import { connectEnvironment } from '../../actions/connectEnvironment';
import { pollActivatingEnvironment } from '../../actions/pollEnvironment';
import { useActionContext } from '../../actions/middleware/useActionContext';
import { BackToEnvironmentsLink } from '../back-to-environments/back-to-environments';

export interface WorkbenchProps extends RouteComponentProps<{ id: string }> {
    liveShareEndpoint: string;
    token: ITokenWithMsalAccount | undefined;
    environmentInfo: ILocalCloudEnvironment | undefined;
    params: URLSearchParams;
    correlationId?: string | null;
    isValidEnvironmentFound?: boolean;
    connectError: string | null;
    connectEnvironment: (
        ...params: Parameters<typeof connectEnvironment>
    ) => ReturnType<typeof connectEnvironment>;
    pollEnvironment: (
        ...params: Parameters<typeof pollActivatingEnvironment>
    ) => ReturnType<typeof pollActivatingEnvironment>;
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

class WorkbenchView extends Component<WorkbenchProps, WorkbenchProps> {
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
        updateFavicon(true);
        if (!this.correlationId) {
            this.correlationId = createUniqueId();
        }

        const { environmentInfo } = this.props;
        this.checkForEnvironmentStatus(environmentInfo);
    }

    checkForEnvironmentStatus(environmentInfo: ILocalCloudEnvironment | undefined) {
        if (!environmentInfo) {
            return;
        }

        if (isEnvironmentAvailable(environmentInfo)) {
            this.cancelPolling();
            this.mountWorkbench(environmentInfo);

            return;
        }

        if (this.state && this.state.connectError) {
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
        if (this.interval) {
            return;
        }

        if (isActivating(environmentInfo)) {
            this.interval = setInterval(() => {
                this.props.pollEnvironment(environmentInfo.id!);
            }, 2000);
        }
    }

    componentWillUnmount() {
        updateFavicon(false);
        this.cancelPolling();
    }

    cancelPolling() {
        if (this.interval) {
            clearInterval(this.interval);
        }
    }

    getLandingPageIfNotReady(environment?: ILocalCloudEnvironment): JSX.Element | undefined {
        // environment is present but its not in available state.
        if (environment && isNotAvailable(environment)) {
            return this.getEnvironmentStatusPage(environment);
        }

        return undefined;
    }

    handleClickToRetry() {
        this.setState({ connectError: null });
    }

    getEnvironmentStatusPage(environment: ILocalCloudEnvironment) {
        let message: string;
        let messageElement: JSX.Element | null = null;
        if (this.state !== null && this.state.connectError !== null) {
            message = `Connecting to environment ${environment.friendlyName} failed. ${this.state.connectError}`;
            messageElement = (
                // tslint:disable-next-line: react-this-binding-issue
                <PrimaryButton onClick={() => this.handleClickToRetry()}>Retry</PrimaryButton>
            );
        } else {
            message = `Environment ${environment.friendlyName} is ${stateToDisplayName(
                environment.state
            ).toLocaleLowerCase()}. Please wait while we connect to your environment.`;
            if (isActivating(environment)) {
                messageElement = <Loader />;
            }
        }

        return (
            <PortalLayout>
                <Stack
                    horizontalAlign='center'
                    verticalFill
                    verticalAlign='center'
                    tokens={{ childrenGap: '20' }}
                >
                    <Stack.Item>
                        <Text>{message}</Text>
                    </Stack.Item>
                    <Stack.Item>{messageElement}</Stack.Item>
                    <Stack.Item>
                        <BackToEnvironmentsLink />
                    </Stack.Item>
                </Stack>
            </PortalLayout>
        );
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

        const quality =
            window.localStorage.getItem('vso-featureset') === 'insider' ? 'insider' : 'stable';

        // We start setting up the LiveShare connection here, so loading workbench assets and creating connection can go in parallel.
        envConnector.ensureConnection(
            environmentInfo,
            accessToken,
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

        const { liveShareEndpoint } = this.props;
        const VSLSWebSocketFactory: IWebSocketFactory = {
            create(url: string) {
                return new VSLSWebSocket(
                    url,
                    accessToken,
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
            connectionToken: vscodeVersion.commit,
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

    private renderWorkBench() {
        const isLoading = this.getLandingPageIfNotReady(this.props.environmentInfo);
        if (isLoading) {
            return isLoading;
        }

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

    render() {
        const content =
            this.props.isValidEnvironmentFound === false ? (
                <PageNotFound />
            ) : (
                this.renderWorkBench()
            );
        return <div className='connect-to-environment'>{content}</div>;
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
        isValidEnvironmentFound:
            !environmentInfo && state.environments.isLoading === false ? false : undefined,
    };
};

const mapDispatch = {
    connectEnvironment,
    pollEnvironment: pollActivatingEnvironment,
};

export const Workbench = connect(getProps, mapDispatch)(WorkbenchView);
