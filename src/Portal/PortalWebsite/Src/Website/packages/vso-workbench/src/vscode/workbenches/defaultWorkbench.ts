import { isHostedOnGithub, getCurrentEnvironmentId } from 'vso-client-core';
import { registerServiceWorker } from 'vso-service-worker-client';
import { getVSCodeVersion } from '../../utils/getVSCodeVersion';
import { AuthenticationError } from '../../errors/AuthenticationError';
import { vsoAPI } from '../../api/vsoAPI';
import { VSCodeWorkbench } from './vscodeWorkbench';
import { EnvConnector } from '../../clients/envConnector';
import { WorkspaceProvider } from '../providers/workspaceProvider/workspaceProvider';
import { UrlCallbackProvider } from '../providers/userDataProvider/urlCallbackProvider';
import { resourceUriProviderFactory } from '../providers/resourceUriProvider/resourceUriProviderFactory';
import {
    EnvironmentsExternalUriProvider,
    PortForwardingExternalUriProvider,
} from '../providers/externalUriProvider/externalUriProvider';
import { credentialsProvider } from '../providers/credentialsProvider/credentialsProvider';
import { URI, IWorkbenchConstructionOptions } from 'vscode-web';
import { telemetry } from '../../telemetry/telemetry';
import { applicationLinksProviderFactory } from '../providers/applicationLinksProvider/applicationLinksProviderFactory';
import { getHomeIndicator } from './getHomeIndicator';
import { getUserDataProvider } from './getUserDataProvider';
import { DEFAULT_GITHUB_VSCODE_AUTH_PROVIDER_ID } from '../../constants';
import { getExtensions } from './getDefaultExtensions';
import { getWorkbenchDefaultLayout } from '../../utils/getWorkbenchDefaultLayout';
import { ensureVSCodeChannelFlag } from '../../utils/ensureVSCodeChannelFlag';

interface IDefaultWorkbenchOptions {
    readonly domElementId: string;
    readonly liveShareEndpoint: string;
    readonly getToken: () => Promise<string | null>;
    readonly onConnection: () => Promise<void>;
    readonly onError?: (e: Error) => any | Promise<any>;
    readonly enableEnvironmentPortForwarding: boolean;
    readonly portForwardingDomainTemplate: string;
}

export class Workbench {
    private workbench: VSCodeWorkbench | null = null;

    constructor(private readonly options: IDefaultWorkbenchOptions) {}

    public connect = async () => {
        const {
            getToken,
            domElementId,
            liveShareEndpoint,
            onConnection,
            onError,
            enableEnvironmentPortForwarding,
            portForwardingDomainTemplate,
        } = this.options;

        try {
            const vscodeConfig = getVSCodeVersion();
            const token = await getToken();
            if (!token) {
                throw new AuthenticationError('Cannot get authentication token.');
            }

            const environmentInfo = await vsoAPI.getEnvironmentInfo(
                getCurrentEnvironmentId(),
                token
            );

            const userDataProvider = await getUserDataProvider();
            const extensions = await getExtensions(userDataProvider.isFirstRun);

            this.workbench = new VSCodeWorkbench({
                domElementId,
                vscodeConfig,
                environmentInfo,
                extensions,
                onConnection,
                getProviders: async (connector: EnvConnector) => {
                    const workspaceProvider = new WorkspaceProvider(
                        new URLSearchParams(location.search),
                        environmentInfo,
                        (url) => url
                    );
                    const urlCallbackProvider = new UrlCallbackProvider();
                    const resourceUriProvider = resourceUriProviderFactory(
                        vscodeConfig.commit,
                        environmentInfo.connection.sessionId,
                        connector
                    );

                    let resolveExternalUri;
                    if (enableEnvironmentPortForwarding) {
                        const ensurePortIsForwarded = connector.ensurePortIsForwarded.bind(
                            connector,
                            environmentInfo,
                            token,
                            liveShareEndpoint
                        );
                        const externalUriProvider = new PortForwardingExternalUriProvider(
                            portForwardingDomainTemplate,
                            environmentInfo.id,
                            ensurePortIsForwarded
                        );
                        resolveExternalUri = externalUriProvider.resolveExternalUri;
                    } else {
                        const externalUriProvider = new EnvironmentsExternalUriProvider(
                            environmentInfo,
                            token,
                            connector,
                            liveShareEndpoint
                        );
                        resolveExternalUri = (uri: URI): Promise<URI> => {
                            return externalUriProvider.resolveExternalUri(uri);
                        };
                    }

                    const resolveCommonTelemetryProperties = telemetry.resolveCommonProperties.bind(
                        telemetry
                    );

                    const applicationLinks = applicationLinksProviderFactory(workspaceProvider);

                    const defaultLayout = getWorkbenchDefaultLayout(
                        environmentInfo,
                        userDataProvider.isFirstRun
                    );

                    const providers: IWorkbenchConstructionOptions = {
                        credentialsProvider,
                        userDataProvider,
                        workspaceProvider,
                        urlCallbackProvider,
                        resourceUriProvider,
                        resolveExternalUri,
                        resolveCommonTelemetryProperties,
                        applicationLinks,
                        homeIndicator: await getHomeIndicator(),
                        enableSyncByDefault: true,
                        authenticationSessionId: DEFAULT_GITHUB_VSCODE_AUTH_PROVIDER_ID,
                        defaultLayout,
                    };

                    return providers;
                },
                getToken,
                liveShareEndpoint: liveShareEndpoint,
            });

            await Promise.all([
                registerServiceWorker({
                    liveShareEndpoint: liveShareEndpoint,
                    features: {
                        useSharedConnection: true,
                    },
                }),
                this.workbench.connect(),
            ]);
        } catch (e) {
            if (onError) {
                return await onError(e);
            }

            throw e;
        }
    };

    public mount = async () => {
        try {
            if (!this.workbench) {
                throw new Error('Connection not initialized, please call "connect" first.');
            }

            await ensureVSCodeChannelFlag();

            await this.workbench.mount();
        } catch (e) {
            const { onError } = this.options;

            if (onError) {
                return await onError(e);
            }

            throw e;
        }
    };
}
