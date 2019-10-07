import { SshChannel } from '@vs/vs-ssh';
import { Disposable, Emitter, Event } from 'vscode-jsonrpc';

import { WebClient } from './client/webClient';
import { WorkspaceClient } from './workspaceClient';
import * as vsls from './contracts/VSLS';
import {
    VSCodeServerHostService,
    VSCodeServerOptions,
    vsCodeServerHostService,
} from './contracts/services';
import { SshChannelOpenner } from './sshChannelOpenner';

import { trace } from '../utils/trace';
import { Signal } from '../utils/signal';

import { DEFAULT_EXTENSIONS, vscodeConfig } from '../constants';
import { ICloudEnvironment } from '../interfaces/cloudenvironment';
import { BrowserSyncService } from './services/browserSyncService';
import { postServiceWorkerMessage } from '../common/post-message';

export type RemoteVSCodeServerDescription = {
    readonly port: number;
    connectionToken: string;
};

export class EnvConnector {
    private initializeConnectionSignal?: Signal<any>;
    private workspaceClient?: Signal<WorkspaceClient>;
    private vscodeServer?: Signal<vsls.SharedServer>;
    private vscodeServerPort?: Signal<number>;

    private disposables: Disposable[] = [];

    private readonly _onVSCodeServerStarted = new Emitter<RemoteVSCodeServerDescription>();
    public readonly onVSCodeServerStarted: Event<RemoteVSCodeServerDescription> = this
        ._onVSCodeServerStarted.event;

    private async startVscodeServer(workspaceClient: WorkspaceClient): Promise<number> {
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
                extensions: [...DEFAULT_EXTENSIONS],
                telemetry: true,
            };

            trace(`Starting VSCode server: `, options);

            const remotePort = await vscodeServerHostClient.startRemoteServerAsync(options);

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
                trace('ServerSharingService closed.');
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

    private async connectWorkspaceClient(
        sessionId: string,
        accessToken: string,
        liveShareEndpoint: string
    ): Promise<WorkspaceClient> {
        if (this.workspaceClient && !this.workspaceClient.isRejected) {
            const workspaceClient = await this.workspaceClient.promise;

            trace('Checking existing workspaceClient connection');
            if (workspaceClient.sshSession && !workspaceClient.sshSession.isClosed) {
                return workspaceClient;
            }

            trace('workspaceClient connection was closed. Creating new connection.');
        }

        this.workspaceClient = new Signal();

        try {
            const webClient = new WebClient(liveShareEndpoint, {
                getToken() {
                    return accessToken;
                },
            });
            const workspaceClient = new WorkspaceClient(webClient);

            await workspaceClient.connect(sessionId);

            postServiceWorkerMessage({
                type: 'cloudenv/update-liveshare-connection-info',
                payload: {
                    sessionId,
                    workspaceInfo: workspaceClient.getWorkspaceInfo()!,
                    workspaceAccess: workspaceClient.getWorkspaceAccess()!,
                },
            });

            await workspaceClient.authenticate();
            await workspaceClient.join();
            await workspaceClient.invokeEnvironmentConfiguration();
            await workspaceClient.registerGitCredentialService();

            const browserSyncService = new BrowserSyncService(workspaceClient);
            await browserSyncService.init();

            this.workspaceClient.complete(workspaceClient);
        } catch (err) {
            this.workspaceClient.reject(err);
        }

        return this.workspaceClient.promise;
    }

    private async getSharedVscodeServer(
        workspaceClient: WorkspaceClient
    ): Promise<vsls.SharedServer> {
        const port = await this.startVscodeServer(workspaceClient);
        trace(`Started VSCode server started on port [${port}].`);

        trace(`Forwarding the VSCode port [${port}].`);
        const vscodeServer = await this.forwardVscodeServerPort(port, workspaceClient);

        return vscodeServer;
    }

    public cleanCachedConnection() {
        this.initializeConnectionSignal = undefined;

        this.dispose();
    }

    public async ensureConnection(
        environmentInfo: ICloudEnvironment,
        accessToken: string,
        liveShareEndpoint: string
    ): Promise<{ sessionPath: string; port: number }> {
        // if already `connecting` or `connected`, return the result
        if (this.initializeConnectionSignal && !this.initializeConnectionSignal.isRejected) {
            return await this.initializeConnectionSignal.promise;
        }

        this.initializeConnectionSignal = new Signal();

        try {
            const { sessionId, sessionPath } = environmentInfo.connection;

            trace(`Live Share session id: ${sessionId}, workspace path: "${sessionPath}"`);
            const workspaceClient = await this.connectWorkspaceClient(
                sessionId,
                accessToken,
                liveShareEndpoint
            );

            const streamManagerClient = workspaceClient.getServiceProxy<vsls.StreamManagerService>(
                vsls.StreamManagerService
            );
            const vscodeServer = await this.getSharedVscodeServer(workspaceClient);

            trace(`Creating the stream.`);
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
            trace(err);

            this.initializeConnectionSignal.reject(err);
        }

        return this.initializeConnectionSignal.promise;
    }

    public async ensurePortIsForwarded(
        environmentInfo: ICloudEnvironment,
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
        trace('Sending handshake request');

        return this.sendRequestInternal(requestStr);
    }

    private async sendRequestInternal(requestStr: string): Promise<SshChannel> {
        if (!this.channelOpener) {
            throw new Error('Initialize connection first to create the port forwarder.');
        }

        const openedChannel = new Signal<SshChannel>();

        try {
            trace('Opening Ssh channel.');
            const channel = await this.channelOpener.openChannel();
            const dataReceivedHandler = channel.onDataReceived((e: Buffer) => {
                // the first request on this channel  is for the handshake,
                // ignore all other data messages as the consumer will set up its own listeners.
                dataReceivedHandler.dispose();

                trace(`Response: \n${e.toString()}\n`);
                channel.adjustWindow(e.length);

                openedChannel.complete(channel);
            });

            this.disposables.push(dataReceivedHandler);

            trace(`Sending the request: \n${requestStr}\n`);
            await channel.send(Buffer.from(requestStr));
        } catch (err) {
            trace('Failed to send request');
            openedChannel.reject(err);
        }

        return openedChannel.promise;
    }

    dispose() {
        this.disposables.forEach((d) => d.dispose());
        this.disposables = [];
    }
}
