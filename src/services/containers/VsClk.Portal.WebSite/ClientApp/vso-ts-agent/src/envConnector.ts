import { SshChannel, PromiseCompletionSource } from '@vs/vs-ssh';
import { Disposable, Emitter, Event, MessageConnection } from 'vscode-jsonrpc';

import {
    vsls,
    wait,
    VSCodeServerHostService,
    vsCodeServerHostService,
    VSCodeServerOptions,
    IEnvironment,
    Signal,
    createTrace,
    IVSCodeConfig
} from 'vso-client-core';

import { WorkspaceClient } from './WorkspaceClient';
import { LiveShareWebClient } from './liveShareWebClient';
import { SshChannelOpenner } from './sshChannelOpenner';

import { tryAuthenticateMessageType, updateLiveShareConnectionInfo } from './service-worker/service-worker-messages';
import { postServiceWorkerMessage } from './service-worker/post-message';
import { onMessage as onServiceWorkerMessage } from './service-worker/serviceWorker';
import { sendTelemetry } from './telemetry/sendTelemetry';

export type RemoteVSCodeServerDescription = {
    readonly port: number;
    connectionToken: string;
};

interface IOnServicesRegistrationEvent {
    workspaceService: vsls.WorkspaceService,
    workspaceClient: WorkspaceClient,
    rpcConnection: MessageConnection,
};

const trace = createTrace(`vso-env-connector`);

export class EnvConnector {
    public remotePort: number | undefined;

    private initializeConnectionSignal?: Signal<any>;
    private workspaceClient?: Signal<WorkspaceClient>;
    private vscodeServer?: Signal<vsls.SharedServer>;
    private vscodeServerPort?: Signal<number>;

    private disposables: Disposable[] = [];

    private readonly _onVSCodeServerStarted = new Emitter<RemoteVSCodeServerDescription>();
    public readonly onVSCodeServerStarted: Event<RemoteVSCodeServerDescription> = this
        ._onVSCodeServerStarted.event;

    constructor(
        private onServicesRegistration: (e: IOnServicesRegistrationEvent) => any
    ) {
        this.disposables.push(
            onServiceWorkerMessage(async (message) => {
                if (
                    message.type !== tryAuthenticateMessageType ||
                    !this.workspaceClient ||
                    !this.workspaceClient.isFulfilled
                ) {
                    return;
                }

                const workspaceClient = await this.workspaceClient.promise;
                postServiceWorkerMessage({
                    type: updateLiveShareConnectionInfo,
                    payload: {
                        sessionId: workspaceClient.getWorkspaceInfo()!.id,
                        workspaceInfo: workspaceClient.getWorkspaceInfo()!,
                        workspaceAccess: workspaceClient.getWorkspaceAccess()!,
                    },
                });
            })
        );
    }

    private startVscodeServer = async (
        workspaceClient: WorkspaceClient,
        vscodeConfig: IVSCodeConfig,
        extensions: string[],
        environmentId: string,
        serviceEndpoint: string,
    ): Promise<number> => {
        if (this.vscodeServerPort && !this.vscodeServerPort.isRejected) {
            // This port will remain shared even if we lose connection.
            return this.vscodeServerPort.promise;
        }
        this.vscodeServerPort = new Signal();

        try {
            const vscodeServerHostClient = workspaceClient.getServiceProxy<VSCodeServerHostService>(
                vsCodeServerHostService
            );

            const options: VSCodeServerOptions = {
                vsCodeCommit: vscodeConfig.commit,
                quality: vscodeConfig.quality,
                extensions,
                telemetry: true,
                environmentId,
                serviceEndpoint,
            };

            trace.info(`Starting VSCode server: `, options);

            const remotePort = await vscodeServerHostClient.startRemoteServerAsync(options);
            this.remotePort = remotePort;

            this._onVSCodeServerStarted.fire({
                port: remotePort,
                connectionToken: vscodeConfig.commit,
            });

            this.vscodeServerPort.complete(remotePort);
        } catch (err) {
            this.vscodeServerPort.reject(err);
        }

        return this.vscodeServerPort.promise;
    }

    private async forwardVscodeServerPort(
        remotePort: number,
        workspaceClient: WorkspaceClient
    ): Promise<vsls.SharedServer> {
        if (this.vscodeServer && !this.vscodeServer.isRejected) {
            return this.vscodeServer.promise;
        }

        this.vscodeServer = new Signal();

        try {
            const serverSharingService = workspaceClient.getServiceProxy<vsls.ServerSharingService>(
                vsls.ServerSharingService
            );
            serverSharingService.connection.onClose(() => {
                trace.info('ServerSharingService closed.');
                this.vscodeServer = undefined;
            });

            const sharedServer = await serverSharingService.startSharingAsync(
                remotePort,
                'VSCodeServerInternal',
                ''
            );

            this.vscodeServer.complete(sharedServer);
        } catch (err) {
            this.vscodeServer.reject(err);
        }

        return this.vscodeServer.promise;
    }

    public async connectWithRetry(
        sessionId: string,
        accessToken: string,
        liveShareEndpoint: string,
        correlationId: string
    ): Promise<WorkspaceClient | undefined> {
        window.performance.mark(`EnvConnector.connectWithRetry ${correlationId}`);
        let workspaceClient;
        // Poll to connect to environment once its available.
        let endPoll = Date.now() + 5 * 60 * 1000; // minutes.
        while (Date.now() < endPoll) {
            try {
                workspaceClient = await this.connectWorkspaceClient(
                    sessionId,
                    accessToken,
                    liveShareEndpoint,
                    true
                );
                break;
            } catch {
                await wait(500);
            }
        }

        window.performance.measure(`EnvConnector.connectWithRetry ${correlationId}`);
        const [measure] = window.performance.getEntriesByName(
            `EnvConnector.connectWithRetry ${correlationId}`
        );

        sendTelemetry('vsonline/portal/connect-with-retry', {
            correlationId: correlationId || '',
            duration: measure.duration,
        });

        return workspaceClient;
    }

    public async connectWorkspaceClient(
        sessionId: string,
        accessToken: string,
        liveShareEndpoint: string,
        skipServiceWorkerNotification = false
    ): Promise<WorkspaceClient> {
        if (this.workspaceClient && !this.workspaceClient.isRejected) {
            const workspaceClient = await this.workspaceClient.promise;

            trace.info('Checking existing workspaceClient connection');
            if (workspaceClient.sshSession && !workspaceClient.sshSession.isClosed) {
                return workspaceClient;
            }

            trace.info('workspaceClient connection was closed. Creating new connection.');
        }

        this.workspaceClient = new Signal();

        try {
            const webClient = new LiveShareWebClient(liveShareEndpoint, {
                getToken() {
                    return accessToken;
                },
            });
            const workspaceClient = new WorkspaceClient(webClient);
            this.disposables.push(workspaceClient);

            await workspaceClient.connect(sessionId);

            if (!skipServiceWorkerNotification) {
                postServiceWorkerMessage({
                    type: updateLiveShareConnectionInfo,
                    payload: {
                        sessionId,
                        workspaceInfo: workspaceClient.getWorkspaceInfo()!,
                        workspaceAccess: workspaceClient.getWorkspaceAccess()!,
                    },
                });
            }

            const clientAuthCompletion = new PromiseCompletionSource<void>();
            await workspaceClient.authenticate(clientAuthCompletion);
            await Promise.all([clientAuthCompletion.promise, workspaceClient.join()]);
            await workspaceClient.invokeEnvironmentConfiguration();

            const workspaceService = workspaceClient.getCurrentWorkspaceClient();
            const rpcConnection = workspaceClient.getCurrentRpcConnection();

            if (!workspaceService) {
                throw new Error('workspaceService not set.');
            }

            if (!rpcConnection) {
                throw new Error('rpcConnection not set.');
            }

            await this.onServicesRegistration({ workspaceService, workspaceClient, rpcConnection });

            this.workspaceClient.complete(workspaceClient);
        } catch (err) {
            this.workspaceClient.reject(err);
        }

        return this.workspaceClient.promise;
    }

    private async getSharedVscodeServer(
        workspaceClient: WorkspaceClient,
        vscodeConfig: IVSCodeConfig,
        extensions: string[],
        environmentId: string,
        serviceEndpoint: string,
    ): Promise<vsls.SharedServer> {
        const port = await this.startVscodeServer(
            workspaceClient,
            vscodeConfig,
            extensions,
            environmentId,
            serviceEndpoint,
        );
        trace.info(`Started VSCode server started on port [${port}].`);

        trace.info(`Forwarding the VSCode port [${port}].`);
        const vscodeServer = await this.forwardVscodeServerPort(port, workspaceClient);

        return vscodeServer;
    }

    public cleanCachedConnection() {
        this.initializeConnectionSignal = undefined;

        this.dispose();
    }

    public async ensureConnection(
        environmentInfo: IEnvironment,
        accessToken: string,
        liveShareEndpoint: string,
        vscodeConfig: IVSCodeConfig,
        extensions: string[],
        environmentId: string,
        serviceEndpoint: string,
    ): Promise<{ sessionPath: string; port: number }> {
        // if already `connecting` or `connected`, return the result
        if (this.initializeConnectionSignal && !this.initializeConnectionSignal.isRejected) {
            return await this.initializeConnectionSignal.promise;
        }

        this.initializeConnectionSignal = new Signal();

        try {
            const { sessionId, sessionPath } = environmentInfo.connection;

            trace.info(`Live Share session id: ${sessionId}, workspace path: "${sessionPath}"`);
            const workspaceClient = await this.connectWorkspaceClient(
                sessionId,
                accessToken,
                liveShareEndpoint
            );

            const streamManagerClient = workspaceClient.getServiceProxy<vsls.StreamManagerService>(
                vsls.StreamManagerService
            );
            const vscodeServer = await this.getSharedVscodeServer(workspaceClient, vscodeConfig, extensions, environmentId, serviceEndpoint);

            trace.info(`Creating the stream.`);
            this.channelOpener = workspaceClient.createServerStream(
                vscodeServer,
                streamManagerClient
            );

            const result = {
                sessionPath,
                port: vscodeServer.sourcePort,
            };

            this.initializeConnectionSignal.complete(result);
        } catch (err) {
            trace.info(err);

            this.initializeConnectionSignal.reject(err);
        }

        return this.initializeConnectionSignal.promise;
    }

    public async ensurePortIsForwarded(
        environmentInfo: IEnvironment,
        accessToken: string,
        port: number,
        liveShareEndpoint: string
    ): Promise<void> {
        const workspaceClient = await this.connectWorkspaceClient(
            environmentInfo.connection.sessionId,
            accessToken,
            liveShareEndpoint
        );

        const servers = await workspaceClient.getSharedServers();

        if (servers.find((s) => s.sourcePort === port)) {
            return;
        }

        const serverSharingClient = await workspaceClient.getServiceProxy<
            vsls.ServerSharingService
        >(vsls.ServerSharingService);

        await serverSharingClient.startSharingAsync(port, `Port ${port}`, '');
    }

    private channelOpener!: SshChannelOpenner;

    public sendHandshakeRequest(requestStr: string): Promise<SshChannel> {
        trace.info('Sending handshake request');

        return this.sendRequestInternal(requestStr);
    }

    private async sendRequestInternal(requestStr: string): Promise<SshChannel> {
        if (!this.channelOpener) {
            throw new Error('Initialize connection first to create the port forwarder.');
        }

        const openedChannel = new Signal<SshChannel>();

        try {
            trace.info('Opening Ssh channel.');
            const channel = await this.channelOpener.openChannel();
            const dataReceivedHandler = channel.onDataReceived((e: Buffer) => {
                // the first request on this channel  is for the handshake,
                // ignore all other data messages as the consumer will set up its own listeners.
                dataReceivedHandler.dispose();

                trace.info(`Response: \n${e.toString()}\n`);
                channel.adjustWindow(e.length);

                openedChannel.complete(channel);
            });

            this.disposables.push(dataReceivedHandler);

            trace.info(`Sending the request: \n${requestStr}\n`);
            await channel.send(Buffer.from(requestStr));
        } catch (err) {
            trace.info('Failed to send request');
            openedChannel.reject(err);
        }

        return openedChannel.promise;
    }

    dispose() {
        if (this.workspaceClient) {
            if (!this.workspaceClient.isFulfilled) {
                this.workspaceClient.cancel();
            }

            this.workspaceClient = undefined;
        }

        if (this.vscodeServerPort) {
            if (!this.vscodeServerPort.isFulfilled) {
                this.vscodeServerPort.cancel();
            }

            this.vscodeServerPort = undefined;
        }

        if (this.vscodeServer) {
            if (!this.vscodeServer.isFulfilled) {
                this.vscodeServer.cancel();
            }

            this.vscodeServer = undefined;
        }

        if (this.initializeConnectionSignal) {
            if (!this.initializeConnectionSignal.isFulfilled) {
                this.initializeConnectionSignal.cancel();
            }

            this.initializeConnectionSignal = undefined;
        }

        this.disposables.forEach((d) => d.dispose());
        this.disposables = [];
    }
}
