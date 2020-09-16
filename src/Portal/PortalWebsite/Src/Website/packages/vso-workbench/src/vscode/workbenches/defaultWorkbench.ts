import { getCurrentEnvironmentId, IEnvironment, IVSCodeConfig } from 'vso-client-core';
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
import { getHomeIndicator } from './getHomeIndicator';
import { getExtensions } from './getDefaultExtensions';
import { getWorkbenchDefaultLayout } from '../../utils/getWorkbenchDefaultLayout';
import { commands } from './workbenchCommands';
import { getProductConfiguration } from './getProductConfiguration';
import { getDefaultSettings } from './getDefaultSettings';
import { authService } from '../../auth/authService';
import { codespaceInitializationTracker } from '../../utils/CodespaceInitializationTracker/CodespaceInitializationTracker';
import { TunnelProvider } from '../providers/tunnelProvider';

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

    public getProviders(
        environmentInfo: IEnvironment,
        vscodeConfig: IVSCodeConfig,
        token: string,
        options: IDefaultWorkbenchOptions,
    ) {
        return async (connector: EnvConnector) => {
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
            if (options.enableEnvironmentPortForwarding) {
                const ensurePortIsForwarded = connector.ensurePortIsForwarded.bind(
                    connector,
                    environmentInfo,
                    token,
                    options.liveShareEndpoint
                );
                const externalUriProvider = new PortForwardingExternalUriProvider(
                    options.portForwardingDomainTemplate,
                    environmentInfo.id,
                    ensurePortIsForwarded
                );
                resolveExternalUri = externalUriProvider.resolveExternalUri;
            } else {
                const externalUriProvider = new EnvironmentsExternalUriProvider(
                    environmentInfo,
                    token,
                    connector,
                    options.liveShareEndpoint
                );
                resolveExternalUri = (uri: URI): Promise<URI> => {
                    return externalUriProvider.resolveExternalUri(uri);
                };
            }

            const resolveCommonTelemetryProperties = telemetry.resolveCommonProperties.bind(
                telemetry
            );

            const defaultLayout = getWorkbenchDefaultLayout(environmentInfo);

            const [
                homeIndicator,
                productConfiguration,
                configurationDefaults,
            ] = await Promise.all([
                getHomeIndicator(),
                getProductConfiguration(),
                getDefaultSettings(),
            ]);

            const providers: IWorkbenchConstructionOptions = {
                credentialsProvider,
                workspaceProvider,
                urlCallbackProvider,
                resourceUriProvider,
                resolveExternalUri,
                tunnelProvider: new TunnelProvider(),
                resolveCommonTelemetryProperties,
                enableSyncByDefault: true,
                configurationDefaults,
                homeIndicator: homeIndicator,
                productConfiguration,
                defaultLayout,
                commands,
            };

            return providers;
        };
    }

    public connect = async () => {
        const { getToken, domElementId, liveShareEndpoint, onConnection, onError } = this.options;

        try {
            const vscodeConfig = await getVSCodeVersion();
            const token = await getToken();
            if (!token) {
                throw new AuthenticationError('Cannot get authentication token.');
            }

            const environmentInfo = await vsoAPI.getEnvironmentInfo(
                getCurrentEnvironmentId(),
                token
            );

            const isFirstLoad = await codespaceInitializationTracker.isFirstCodespaceLoad();
            const extensions = await getExtensions(isFirstLoad);
            const providersFunc = this.getProviders(
                environmentInfo,
                vscodeConfig,
                token,
                this.options,
            );

            this.workbench = new VSCodeWorkbench({
                domElementId,
                vscodeConfig,
                environmentInfo,
                extensions,
                onConnection,
                getProviders: providersFunc,
                getToken,
                liveShareEndpoint,
            });

            await Promise.all([
                registerServiceWorker({
                    liveShareEndpoint,
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
