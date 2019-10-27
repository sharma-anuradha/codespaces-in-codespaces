import * as vsls from './contracts/VSLS';
import * as rpc from 'vscode-jsonrpc';
import * as ssh from '@vs/vs-ssh';
import { RpcProxy } from './RpcProxy';
import { ILiveShareClient, IWorkspaceInfo, IWorkspaceAccess } from './client/ILiveShareClient';
import { SshChannelOpenner } from './sshChannelOpenner';
import { trace as baseTrace } from '../utils/trace';
import {
    EnvironmentConfigurationService,
    environmentConfigurationService,
} from './contracts/services';
import { GitCredentialService } from './services/gitCredentialService';

const info = baseTrace.extend('workspace-client:info');

const packageJson = {
    name: 'sw-port-tunnel',
    displayName: 'Service Worker Port Tunnel',
    description: 'Port forwarding thru the Service Worker',
    version: '0.1.0-dev',
};

type RpcProxyFor<T> = T & RpcProxy;

export class WorkspaceClient implements rpc.Disposable {
    private workspaceInfo?: IWorkspaceInfo;
    private workspaceAccess?: IWorkspaceAccess;
    private socketStream?: ssh.Stream;
    private rpcConnection?: rpc.MessageConnection;
    private workspaceClient?: vsls.WorkspaceService;
    private sessionInfo?: vsls.WorkspaceSessionInfo;

    public sshSession?: ssh.SshClientSession;

    public constructor(public readonly webClient: ILiveShareClient) {}

    // tslint:disable-next-line: informative-docs
    /** internal */ get internalPortName() {
        return 'VSCodeServerInternal';
    }

    // tslint:disable-next-line: informative-docs
    /** internal */ getWorkspaceInfo() {
        return this.workspaceInfo;
    }

    public getWorkspaceAccess() {
        return this.workspaceAccess;
    }

    public async connect(invitationId: string): Promise<void> {
        this.workspaceInfo = (await this.webClient.getWorkspaceInfo(invitationId)) || undefined;
        if (!this.workspaceInfo) {
            throw new Error('Workspace not found: ' + invitationId);
        }

        this.workspaceAccess =
            (await this.webClient.getWorkspaceAccess(this.workspaceInfo.id)) || undefined;

        if (!this.workspaceAccess) {
            throw new Error('Workspace not found: ' + invitationId);
        }

        this.socketStream = await this.openConnection(this.workspaceInfo);

        // Prevent an old connection from being re-used.
        this.sshSession = undefined;
        this.rpcConnection = undefined;
    }

    public async authenticate(): Promise<void> {
        if (!this.workspaceInfo || !this.workspaceAccess || !this.socketStream) {
            throw new Error('Connect to a workspace first.');
        }

        const config = new ssh.SshSessionConfiguration();
        config.keyExchangeAlgorithms.splice(0);
        config.keyExchangeAlgorithms.push(ssh.SshAlgorithms.keyExchange.dhGroup14Sha256);
        
        this.sshSession = new ssh.SshClientSession(this.socketStream, config);

        // The client authenticates over SSH using the workspace session token.
        this.sshSession.setPasswordCredential('', this.workspaceAccess.sessionToken);

        // The server authenticates over SSH via a public key.
        this.sshSession.onAuthenticating((e) => {
            // At this point the SSH protocol has already validated that the server holds
            // the private key that corresponds to the public key in e.key. So we just need
            // to check if the public key matches one of the host keys published for the workspace.
            e.authenticationPromise = this.authenticateServer(e.key!);
        });

        if (!(await this.sshSession.authenticate())) {
            throw new Error('Failed to authenticate with the remote host.');
        }
    }

    private async authenticateServer(serverKey: ssh.KeyPair): Promise<object | null> {
        const rsa = ssh.SshAlgorithms.publicKey.rsaWithSha512!;
        const serverKeyBytes = (await serverKey.getPublicKeyBytes())!;

        for (let knownHostKey of (this.workspaceInfo && this.workspaceInfo.hostPublicKeys) || []) {
            // Get the public key bytes using the matching algorithm name to ensure a valid comparison.
            const hostKey = rsa.createKeyPair();
            await hostKey.setPublicKeyBytes(Buffer.from(knownHostKey, 'base64'));
            const hostKeyBytes = (await hostKey.getPublicKeyBytes(serverKey.algorithmName))!;

            if (serverKeyBytes.equals(hostKeyBytes)) {
                // Returning a non-null object indicates successful authentication.
                // (We're not using the principal here.)
                const serverPrincipal = {};
                return serverPrincipal;
            }
        }

        return null;
    }

    public async join(): Promise<void> {
        if (!this.workspaceInfo || !this.workspaceAccess || !this.sshSession) {
            throw new Error('Connect to a workspace and authenticate first.');
        }

        if (!this.rpcConnection) {
            const channel = await this.sshSession.openChannel();
            const rpcStream = new ssh.SshRpcMessageStream(channel);
            this.rpcConnection = rpc.createMessageConnection(rpcStream.reader, rpcStream.writer);
            this.rpcConnection.listen();

            const configClient = this.getServiceProxy<vsls.ConfigurationService>(
                vsls.ConfigurationService
            );
            const hostVersionInfo = await configClient.exchangeVersionsAsync(
                {},
                {
                    // TODO: Report VS Code applicationName / applicationVersion
                    extensionName: packageJson.name,
                    extensionVersion: packageJson.version,
                }
            );
            info('Host version: ' + JSON.stringify(hostVersionInfo));
        }

        this.workspaceClient = this.getServiceProxy<vsls.WorkspaceService>(vsls.WorkspaceService);

        var clientCapabilities = new vsls.ClientCapabilities();
        clientCapabilities.isNonInteractive = true;

        this.sessionInfo = await this.workspaceClient.joinWorkspaceAsync({
            id: this.workspaceInfo.id,
            connectionMode: vsls.ConnectionMode.Local, // Note "local" connection mode is correct when talking to remote service.
            joiningUserSessionToken: this.workspaceAccess.sessionToken,
            clientCapabilities: clientCapabilities            
        });
    }

    public async invokeEnvironmentConfiguration() {
        const environmentConfiguration = await this.getServiceProxy<
            EnvironmentConfigurationService
        >(environmentConfigurationService);
        try {
            await environmentConfiguration.configureEnvironmentAsync();
        } catch (e) {
            info('Configure Environments failed to respond. ', e);
        }
    }

    public async registerGitCredentialService() {
        // Expose credential service
        const gitCredentialService = new GitCredentialService(
            this.workspaceClient!,
            this.rpcConnection!
        );
        await gitCredentialService.shareService();
    }

    public async disconnect(): Promise<void> {
        const workspaceClient = this.workspaceClient;
        this.workspaceClient = undefined;

        if (this.sessionInfo) {
            const sessionInfo = this.sessionInfo;
            this.sessionInfo = undefined;
            await workspaceClient!.unjoinWorkspaceAsync(sessionInfo.id!);
        }
    }

    dispose(): void {
        this.disconnect();
    }

    public getServiceProxy<T>(serviceInfo: vsls.ServiceInfo<T>): RpcProxyFor<T> {
        return RpcProxy.create<T>(serviceInfo, this.rpcConnection!) as RpcProxyFor<T>;
    }

    public createServerStream(
        server: vsls.SharedServer,
        streamManagerClient: vsls.StreamManagerService
    ) {
        return new SshChannelOpenner(server, this.sshSession!, streamManagerClient);
    }

    public async getSharedServers(): Promise<vsls.SharedServer[]> {
        const serverSharingClient = await this.getServiceProxy<vsls.ServerSharingService>(
            vsls.ServerSharingService
        );
        const sharedServers: vsls.SharedServer[] = await serverSharingClient.getSharedServersAsync();
        let servers: vsls.SharedServer[] = [];
        sharedServers.forEach((server) => {
            if (server.sessionName !== this.internalPortName) {
                servers.push(server);
            }
        });
        return servers;
    }

    private openConnection(workspace: IWorkspaceInfo): Promise<ssh.Stream> {
        if (!workspace.relayLink) {
            throw new Error('Workspace does not have a relay endpoint.');
        }

        // Reference:
        // https://github.com/Azure/azure-relay-node/blob/7b57225365df3010163bf4b9e640868a02737eb6/hyco-ws/index.js#L107-L137
        const relayUri =
            workspace.relayLink.replace('sb:', 'wss:').replace('.net/', '.net:443/$hc/') +
            '?sb-hc-action=connect&sb-hc-token=' +
            encodeURIComponent(workspace.relaySas || '');

        // There are two relay websocket implementations below:
        //   1) Using the browser (W3C) websocket API adapter provided by the node-websocket package.
        //      This code is kept for future compatibility with browser (VS Online) clients.
        //   2) Using the node-websocket API directly
        //      This enables better error diagnostic information and therefore is preferred.
        const socket = new WebSocket(relayUri);
        socket.binaryType = 'arraybuffer';
        return new Promise<ssh.Stream>((resolve, reject) => {
            socket.onopen = () => {
                resolve(new ssh.WebSocketStream(socket));
            };
            socket.onerror = (e) => {
                reject(new Error('Failed to connect to relay: ' + relayUri));
            };
        });
    }
}
