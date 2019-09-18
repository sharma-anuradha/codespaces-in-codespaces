import { LiveShareConnectionFactory, LiveShareConnection } from './connection-factory';
import { SshChannel } from '@vs/vs-ssh';
import { Logger, createLogger } from './logger';
import { CredentialsManager } from './credentials-manager';
import { ConnectionDetails } from '../../common/connection-details';

type ConnectionRequest = {
    readonly requestId?: string;
    readonly sessionId: string;
};

export class ConnectionManager {
    private readonly logger: Logger;

    private readonly connectionStore = new Map<string, Promise<LiveShareConnection>>();

    constructor(
        private readonly connectionFactory: LiveShareConnectionFactory,
        private readonly credentialsManager: CredentialsManager
    ) {
        this.logger = createLogger('ConnectionManager');
    }

    initializeConnection(payload: ConnectionDetails) {
        this.getConnectionFor(payload);
    }

    async getChannelFor(connectionRequest: ConnectionRequest, port: number): Promise<SshChannel> {
        const connection = await this.getConnectionFor(connectionRequest);
        const portForwarder = await connection.getSharedServerStream(port);
        const channel = await portForwarder.openChannel();

        connection.onClose((reason) => {
            this.logger.info('Closing channel', {
                ...connectionRequest,
                reason,
            });

            channel.close();
            channel.dispose();
        });

        return channel;
    }

    async disposeConnection(connectionRequest: ConnectionRequest) {
        if (this.connectionStore.has(connectionRequest.sessionId)) {
            try {
                const connection = await this.connectionStore.get(connectionRequest.sessionId)!;
                connection.dispose();
            } catch (error) {
                this.logger.error('Failed to dispose connection.', {
                    ...connectionRequest,
                    error,
                });
            } finally {
                this.logger.verbose('Deleting connection from connectionStore.', {
                    ...connectionRequest,
                    reason: 'dispose connection',
                });
                this.connectionStore.delete(connectionRequest.sessionId);
            }
        }
    }

    private async getConnectionFor(
        connectionRequest: ConnectionRequest
    ): Promise<LiveShareConnection> {
        this.logger.verbose('Get connection for', connectionRequest);

        if (!this.connectionStore.has(connectionRequest.sessionId)) {
            const connection = this.createConnectionFor(connectionRequest);

            this.logger.verbose('Storing connection in connectionStore.', connectionRequest);
            this.connectionStore.set(connectionRequest.sessionId, connection);

            connection.then(
                (connection) => {
                    connection.onClose((reason) => {
                        this.logger.verbose('Closed connection.', {
                            ...connectionRequest,
                            reason,
                        });

                        this.logger.verbose('Deleting connection from connectionStore.', {
                            ...connectionRequest,
                            reason: 'connection closed',
                        });
                        this.connectionStore.delete(connectionRequest.sessionId);
                    });

                    return connection;
                },
                (error) => {
                    this.logger.error('Failed to create connection', {
                        ...connectionRequest,
                        error,
                    });

                    this.logger.verbose('Deleting connection from connectionStore.', {
                        ...connectionRequest,
                        reason: 'connection promise rejected',
                    });
                    this.connectionStore.delete(connectionRequest.sessionId);

                    throw error;
                }
            );

            return connection;
        }

        this.logger.verbose('Getting connection from connectionStore.', connectionRequest);
        return this.connectionStore.get(connectionRequest.sessionId)!;
    }

    private async createConnectionFor(connectionRequest: ConnectionRequest) {
        const credentials = this.credentialsManager.getCredentials(connectionRequest.sessionId);

        if (!credentials) {
            throw new Error('Cannot create connections before credentials are set.');
        }

        return this.connectionFactory.createConnection(connectionRequest.sessionId);
    }
}
