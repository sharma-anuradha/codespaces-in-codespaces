import * as vsls from './contracts/VSLS';
import * as rpc from 'vscode-jsonrpc';
import * as ssh from '@vs/vs-ssh';
import { RpcProxy } from './RpcProxy';
import { WebClient, WorkspaceInfo, WorkspaceAccess } from './webClient';
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
    private workspaceInfo?: WorkspaceInfo;
    private workspaceAccess?: WorkspaceAccess;
    private socketStream?: ssh.Stream;
    private rpcConnection?: rpc.MessageConnection;
    private workspaceClient?: vsls.WorkspaceService;
    private sessionInfo?: vsls.WorkspaceSessionInfo;

    public sshSession?: ssh.SshClientSession;

    public constructor(public readonly webClient: WebClient) {}

    public get serviceUri() {
        return this.webClient.serviceUri;
    }

    public get internalPortName() {
        return 'VSCodeServerInternal';
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

        this.socketStream = await this.webClient.openConnection(this.workspaceInfo);

        // Prevent an old connection from being re-used.
        this.sshSession = undefined;
        this.rpcConnection = undefined;
    }

    public async authenticate(): Promise<void> {
        if (!this.workspaceInfo || !this.workspaceAccess || !this.socketStream) {
            throw new Error('Connect to a workspace first.');
        }

        this.sshSession = new ssh.SshClientSession(
            this.socketStream,
            new ssh.SshSessionConfiguration()
        );

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

        this.sessionInfo = await this.workspaceClient.joinWorkspaceAsync({
            id: this.workspaceInfo.id,
            connectionMode: vsls.ConnectionMode.Local, // Note "local" connection mode is correct when talking to remote service.
            joiningUserSessionToken: this.workspaceAccess.sessionToken,
        });

        // Expose credential service
        const gitCredentialService = new GitCredentialService(
            this.workspaceClient,
            this.rpcConnection
        );
        await gitCredentialService.shareService();
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
}
