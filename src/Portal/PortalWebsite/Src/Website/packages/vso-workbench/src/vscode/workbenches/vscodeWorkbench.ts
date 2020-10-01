import * as debug from 'debug';

import { IVSCodeConfig, IEnvironment, randomString, vsls, VSCS_FEATURESET_LOCALSTORAGE_KEY} from 'vso-client-core';
import { IWebSocketFactory, IWorkbenchConstructionOptions, IProductQualityChangeHandler } from 'vscode-web';
import { BrowserConnectorMessages } from 'vso-ts-agent';

import { postServiceWorkerMessage, disconnectCloudEnv } from 'vso-service-worker-client';

import { EnvConnector } from '../../clients/envConnector';
import { VSLSWebSocket } from '../../clients/VSLSWebSocket';

import { AuthenticationError } from '../../errors/AuthenticationError';
import { vsoAPI } from '../../api/vsoAPI';

import { authService } from '../../auth/authService';
import { vscode } from '../vscodeAssets/vscode';
import { config } from '../../config/config';
import { getUriAuthority } from '../../utils/getUriAuthority';
import { GitCredentialService } from '../../rpcServices/GitCredentialService';
import { BrowserSyncService } from '../../rpcServices/BrowserSyncService';
import { assertValidSubdomain } from '../../utils/assertValidSubdomain';

interface IWorkbenchOptions {
    domElementId: string;
    vscodeConfig: IVSCodeConfig;
    extensions: string[];
    getToken: () => Promise<string | null>;
    environmentInfo: IEnvironment;
    liveShareEndpoint: string;
    getProviders: (connector: EnvConnector) => Promise<IWorkbenchConstructionOptions>;
    onConnection: () => Promise<void>;
}

export const trace = debug.default('vso-workbench');

const TRACE_NAME = 'vsls-web-socket';
const logContent = trace.extend(`${TRACE_NAME}:trace:content`);
logContent.log =
    // tslint:disable-next-line: no-console
    typeof console.debug === 'function' ? console.debug.bind(console) : console.log.bind(console);

export class VSCodeWorkbench {
    private envConnector: EnvConnector | null = null;

    constructor(private readonly options: IWorkbenchOptions) {}

    public connect = async () => {
        const { getToken, onConnection } = this.options;

        const token = await getToken();

        if (!token) {
            throw new AuthenticationError('No token found.');
        }

        if (!this.envConnector) {
            this.envConnector = new EnvConnector(async (e) => {
                const { workspaceClient, workspaceService, rpcConnection } = e;

                // Expose credential service
                const gitCredentialService = new GitCredentialService(
                    workspaceService,
                    rpcConnection
                );
                await gitCredentialService.shareService();

                // Expose browser sync service
                const sourceEventService = workspaceClient.getServiceProxy<vsls.SourceEventService>(
                    vsls.SourceEventService
                );

                new BrowserSyncService(sourceEventService);

                const codespaceInfo = await authService.getPartnerInfo();
                if (!codespaceInfo) {
                    return;
                }

                /**
                 * If no `homeIndicator` present in `CodespaceInfo`,
                 * enable the `Go Home` item in the VSCode FileMenu,
                 * since vscode won't add it automatically.
                 * https://github.com/github/codespaces/issues/1014
                 */
                if (!('homeIndicator' in codespaceInfo)) {
                    await sourceEventService.fireEventAsync(
                        BrowserConnectorMessages.GetLocalStorageValueResponse,
                        ''
                    );
                }
            });
        }

        await vscode.getVSCode();

        /**
         * Temporary comment out since doing this will cause the "Failed to start VSCode server"
         * errors. We need to do more work on the connector side to enable the concurrent connection.
         */
        // // We start setting up the LiveShare connection here,
        // // so loading workbench assets and creating connection can go in parallel.
        // await Promise.all([
        //     vscode.getVSCode(),
        //     this.envConnector.ensureConnection(
        //         environmentInfo,
        //         token,
        //         liveShareEndpoint,
        //         vscodeConfig,
        //         extensions,
        //         `${config.api}/environments`
        //     ),
        // ]);

        await onConnection();
    };

    public mount = async () => {
        const {
            getToken,
            domElementId,
            environmentInfo,
            liveShareEndpoint,
            vscodeConfig,
            extensions,
            getProviders,
        } = this.options;

        const token = await getToken();

        if (!token) {
            throw new AuthenticationError('No token found.');
        }

        const workbenchEl = document.querySelector<HTMLElement>(`#${domElementId}`);
        if (!workbenchEl) {
            throw new Error(`No VSCode workbench root DOM element found (#${domElementId}).`);
        }

        if (!vscode) {
            throw new Error(`Cannot get VSCode Workbench.`);
        }

        const connector = this.envConnector;
        if (!connector) {
            throw new Error('Call "initializeConnection" first.');
        }

        const VSLSWebSocketFactory: IWebSocketFactory = {
            create(url: string) {
                assertValidSubdomain(environmentInfo);

                return new VSLSWebSocket(
                    url,
                    token,
                    liveShareEndpoint,
                    randomString(),
                    vscodeConfig,
                    connector,
                    logContent,
                    async (id: string) => {
                        const authToken = await authService.getCachedToken();
                        if (!authToken) {
                            throw new AuthenticationError('Cannot get auth token.');
                        }

                        return await vsoAPI.getEnvironmentInfo(id, authToken);
                    },
                    extensions,
                    `${config.api}/environments`
                );
            },
        };

        const ProductQualityChangeHandler: IProductQualityChangeHandler = (newQuality: string) => {
            window.localStorage.setItem(VSCS_FEATURESET_LOCALSTORAGE_KEY, newQuality);
            window.location.reload();
        };

        const providers = await getProviders(connector);
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

        const workbenchConfig: IWorkbenchConstructionOptions = {
            remoteAuthority: getUriAuthority(environmentInfo),
            webSocketFactory: VSLSWebSocketFactory,
            connectionToken: vscodeConfig.commit,
            productQualityChangeHandler: ProductQualityChangeHandler,
            ...providers,
        };

        trace(`Creating workbench on #${domElementId}, with config: `, workbenchConfig);
        vscode.create(workbenchEl, workbenchConfig);
    };
}
