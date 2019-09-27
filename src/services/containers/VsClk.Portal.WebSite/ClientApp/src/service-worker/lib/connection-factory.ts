import { WebClient } from '../../ts-agent/webClient';
import { WorkspaceClient } from '../../ts-agent/workspaceClient';
import * as vsls from '../../ts-agent/contracts/VSLS';
import { Event, Emitter, Disposable } from 'vscode-jsonrpc';
import { SshDisconnectReason } from '@vs/vs-ssh';
import { createLogger, Logger } from './logger';
import { ConfigurationManager } from './configuration-manager';
import { CredentialsManager } from './credentials-manager';

export class LiveShareConnection implements Disposable {
    private serverSharingService!: vsls.ServerSharingService;
    private streamManagerClient!: vsls.StreamManagerService;
    private workspaceClient!: WorkspaceClient;

    private readonly logger: Logger;

    private readonly _onClose = new Emitter<SshDisconnectReason>();
    public readonly onClose: Event<SshDisconnectReason> = this._onClose.event;

    private readonly _onError = new Emitter<Error>();
    public readonly onError: Event<Error> = this._onError.event;

    private disposables: Disposable[] = [];

    constructor(
        private readonly credentialsManager: CredentialsManager,
        private readonly configurationManager: ConfigurationManager,
        private sessionId: string
    ) {
        this.logger = createLogger(`LiveShareConnection:${sessionId.substr(0, 5)}`);
    }

    async init() {
        const defaultArgs = {
            liveShareUri: this.configurationManager.configuration.liveShareEndpoint,
            sessionId: this.sessionId,
        };
        this.logger.info('Initializing live share connection', defaultArgs);

        const credentials = this.credentialsManager.getCredentials(this.sessionId);

        if (!credentials) {
            this.logger.error('Cannot create connection. Missing credentials.', defaultArgs);
            throw new Error('Cannot create connection. Missing credentials.');
        }

        const webClient = new WebClient(
            this.configurationManager.configuration.liveShareEndpoint,
            credentials.token
        );
        this.workspaceClient = new WorkspaceClient(webClient);
        this.disposables.push(this.workspaceClient);

        if (this.workspaceClient.sshSession) {
            this.disposables.push(
                this.workspaceClient.sshSession.onClosed((event) => {
                    this.dispose();

                    if (event.error) {
                        this._onError.fire(event.error);
                    }

                    this._onClose.fire(event.reason);
                })
            );
        }

        try {
            this.logger.verbose('Connecting to live share session.', defaultArgs);
            await this.workspaceClient.connect(this.sessionId);
            this.logger.verbose('Authenticating live share session.', defaultArgs);
            await this.workspaceClient.authenticate();
            this.logger.verbose('Joining live share session.', defaultArgs);
            await this.workspaceClient.join();

            this.logger.info('Initialized live share session.', defaultArgs);
        } catch (error) {
            this.logger.error('Failed to create Live Share connection.', {
                ...defaultArgs,
                error,
            });

            this.workspaceClient.dispose();

            throw error;
        }
    }

    async getSharedServerStream(port: number) {
        const defaultArgs = {
            liveShareUri: this.configurationManager.configuration.liveShareEndpoint,
            sessionId: this.sessionId,
        };
        this.logger.verbose('Getting shared server stream.', defaultArgs);

        this.serverSharingService = this.workspaceClient.getServiceProxy<vsls.ServerSharingService>(
            vsls.ServerSharingService
        );
        this.streamManagerClient = this.workspaceClient.getServiceProxy<vsls.StreamManagerService>(
            vsls.StreamManagerService
        );

        const sharedServers: vsls.SharedServer[] = await this.serverSharingService.getSharedServersAsync();
        const targetServer = sharedServers.find((server) => {
            return server.sourcePort === port;
        });

        if (!targetServer) {
            throw new Error(`VSCode server port ${port} not shared.`);
        }

        const localPortForwarder = this.workspaceClient.createServerStream(
            targetServer,
            this.streamManagerClient
        );

        this.logger.verbose('Got shared server stream.', defaultArgs);

        return localPortForwarder;
    }

    dispose() {
        this.disposables.forEach((disposable) => {
            disposable.dispose();
        });
    }
}

export class LiveShareConnectionFactory {
    private readonly logger: Logger;
    constructor(
        private readonly credentialsManager: CredentialsManager,
        private readonly configurationManager: ConfigurationManager
    ) {
        this.logger = createLogger('LiveShareConnectionFactory');
    }

    async createConnection(sessionId: string) {
        const defaultAttributes = {
            sessionId,
        };
        this.logger.info('Creating LiveShare connection.', defaultAttributes);

        const connection = new LiveShareConnection(
            this.credentialsManager,
            this.configurationManager,
            sessionId
        );

        try {
            await connection.init();

            return connection;
        } catch (error) {
            this.logger.info('Failed to initialize LiveShare connection.', {
                ...defaultAttributes,
                error,
            });

            throw error;
        }
    }
}
