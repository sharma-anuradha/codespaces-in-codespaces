import { isHostedOnGithub, getCurrentEnvironmentId } from 'vso-client-core';
import { DEFAULT_EXTENSIONS, HOSTED_IN_GITHUB_EXTENSIONS } from '../../constants';
import { UserDataProvider } from '../providers/userDataProvider/userDataProvider';
import { getVSCodeVersion } from '../../utils/getVSCodeVersion';
import { AuthenticationError } from '../../errors/AuthenticationError';
import { vsoAPI } from '../../api/vsoAPI';
import { VSCodeWorkbench } from './vscodeWorkbench';
import { EnvConnector, registerServiceWorker } from 'vso-ts-agent';
import { WorkspaceProvider } from '../providers/workspaceProvider/workspaceProvider';
import { UrlCallbackProvider } from '../providers/userDataProvider/urlCallbackProvider';
import { resourceUriProviderFactory } from '../providers/resourceUriProvider/resourceUriProviderFactory';
import { EnvironmentsExternalUriProvider } from '../providers/externalUriProvider/externalUriProvider';
import { credentialsProvider } from '../providers/credentialsProvider/credentialsProvider';
import { URI } from 'vscode-web';
import { telemetry } from '../../telemetry/telemetry';
import { applicationLinksProviderFactory } from '../providers/applicationLinksProvider/applicationLinksProviderFactory';

export const getExtensions = (): string[] => {
    // TODO: move the extensions into the platform info payload instead
    return (!isHostedOnGithub())
        ? [...DEFAULT_EXTENSIONS]
        : [...DEFAULT_EXTENSIONS, ...HOSTED_IN_GITHUB_EXTENSIONS];
};

const getUserDataProvider = async () => {
    const defaultSettings = isHostedOnGithub() ? '{"workbench.colorTheme": "Github"}' : '';

    const userDataProvider = new UserDataProvider(defaultSettings);
    await userDataProvider.initializeDBProvider();

    return userDataProvider;
};

interface IDefaultWorkbenchOptions {
    domElementId: string;
    liveShareEndpoint: string;
    getToken: () => Promise<string | null>;
    onConnection: () => Promise<void>;
    onError?: (e: Error) => any | Promise<any>;
}

export class Workbench {
    private workbench: VSCodeWorkbench | null = null;

    constructor(private readonly options: IDefaultWorkbenchOptions) {}

    public connect = async () => {
        const { getToken, domElementId, liveShareEndpoint, onConnection, onError } = this.options;

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

            this.workbench = new VSCodeWorkbench({
                domElementId,
                vscodeConfig,
                environmentInfo,
                extensions: getExtensions(),
                onConnection,
                getProviders: async (connector: EnvConnector) => {
                    const userDataProvider = await getUserDataProvider();
                    const workspaceProvider = new WorkspaceProvider(
                        new URLSearchParams(location.search),
                        environmentInfo,
                        url => url
                    );
                    const urlCallbackProvider = new UrlCallbackProvider();
                    const resourceUriProvider = resourceUriProviderFactory(
                        vscodeConfig.commit,
                        environmentInfo.connection.sessionId,
                        connector
                    );

                    const externalUriProvider = new EnvironmentsExternalUriProvider(
                        environmentInfo,
                        token,
                        connector,
                        liveShareEndpoint
                    );

                    const resolveExternalUri = (uri: URI): Promise<URI> => {
                        return externalUriProvider.resolveExternalUri(uri);
                    };

                    const resolveCommonTelemetryProperties = telemetry.resolveCommonProperties.bind(
                        telemetry
                    );

                    const applicationLinks = applicationLinksProviderFactory(workspaceProvider);

                    return {
                        credentialsProvider,
                        userDataProvider,
                        workspaceProvider,
                        urlCallbackProvider,
                        resourceUriProvider,
                        resolveExternalUri,
                        resolveCommonTelemetryProperties,
                        applicationLinks,
                    };
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
