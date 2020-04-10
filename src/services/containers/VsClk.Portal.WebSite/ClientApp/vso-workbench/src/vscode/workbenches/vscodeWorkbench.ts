import * as debug from 'debug';

import { IVSCodeConfig, IEnvironment, randomString } from 'vso-client-core';
import { IWebSocketFactory, IWorkbenchConstructionOptions } from 'vscode-web';

import {
    EnvConnector,
    VSLSWebSocket,
    postServiceWorkerMessage,
    disconnectCloudEnv,
} from 'vso-ts-agent';

import { AuthenticationError } from '../../errors/AuthenticationError';
import { vsoAPI } from '../../api/vsoAPI';

import { authService } from '../../auth/authService';
import { vscode } from '../vscodeAssets/vscode';
import { getCurrentEnvironmentId } from '../../utils/getCurrentEnvironmentId';
import { config } from '../../config/config';

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
        const {
            getToken,
            environmentInfo,
            liveShareEndpoint,
            vscodeConfig,
            extensions,
            onConnection,
        } = this.options;

        const token = await getToken();

        if (!token) {
            throw new AuthenticationError('No token found.');
        }

        if (!this.envConnector) {
            this.envConnector = new EnvConnector(() => {});
        }

        // We start setting up the LiveShare connection here,
        // so loading workbench assets and creating connection can go in parallel.
        await Promise.all([
            vscode.getVSCode(),
            this.envConnector.ensureConnection(
                environmentInfo,
                token,
                liveShareEndpoint,
                vscodeConfig,
                extensions,
                getCurrentEnvironmentId(),
                `${config.api}/environments`
            ),
        ]);

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
                return new VSLSWebSocket(
                    url,
                    token,
                    environmentInfo,
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
                    getCurrentEnvironmentId(),
                    `${config.api}/environments`
                );
            },
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
            remoteAuthority: `vsonline+${environmentInfo.id}`,
            webSocketFactory: VSLSWebSocketFactory,
            connectionToken: vscodeConfig.commit,
            ...providers,
        };

        trace(`Creating workbench on #${domElementId}, with config: `, workbenchConfig);
        vscode.create(workbenchEl, workbenchConfig);
    };
}
