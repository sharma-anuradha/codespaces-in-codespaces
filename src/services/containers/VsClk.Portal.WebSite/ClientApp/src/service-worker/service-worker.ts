import { InMemoryLiveShareClient } from '../ts-agent/client/inMemoryClient';
import { WebClient } from '../ts-agent/client/webClient';

import {
    authenticateMessageType,
    configureServiceWorker,
    disconnectCloudEnv,
    ServiceWorkerMessage,
    updateLiveShareConnectionInfo,
} from '../common/service-worker-messages';

import { createLogger } from './lib/logger';
import { ServiceRegistry } from './lib/service-registry';
import { ConfigurationManager } from './lib/configuration-manager';
import { CredentialsManager } from './lib/credentials-manager';
import { LiveShareConnectionFactory } from './lib/connection-factory';
import { ConnectionManager } from './lib/connection-manager';
import { PassThroughHttpClient, LiveShareHttpClient } from './lib/http-client';

declare var self: ServiceWorkerGlobalScope;

export const logger = createLogger();

const serviceRegistry = new ServiceRegistry();

serviceRegistry.registerInstance('ConfigurationManager', new ConfigurationManager());
serviceRegistry.registerInstance('CredentialsManager', new CredentialsManager());
serviceRegistry.registerFactory('LiveShareClient', (serviceRegistry) => {
    const configurationManager = serviceRegistry.getInstance('ConfigurationManager');
    if (configurationManager.configuration.features.useSharedConnection) {
        return new InMemoryLiveShareClient();
    }

    const credentialsManager = serviceRegistry.getInstance('CredentialsManager');
    return new WebClient(configurationManager.configuration.liveShareEndpoint, {
        getToken(sessionId: string) {
            const credentials = credentialsManager.getCredentials(sessionId);
            if (!credentials) {
                throw new Error('No credentials.');
            }
            return credentials.token;
        },
    });
});
serviceRegistry.registerFactory('ConnectionFactory', (serviceRegistry) => {
    return new LiveShareConnectionFactory(
        serviceRegistry.getInstance('LiveShareClient'),
        serviceRegistry.getInstance('ConfigurationManager')
    );
});
serviceRegistry.registerFactory('ConnectionManager', (serviceRegistry) => {
    const connectionFactory = serviceRegistry.getInstance('ConnectionFactory');
    return new ConnectionManager(connectionFactory);
});
serviceRegistry.registerFactory('HttpClient', (serviceRegistry) => {
    if (!serviceRegistry.canResolve('LiveShareClient')) {
        return new PassThroughHttpClient();
    }
    return new LiveShareHttpClient(serviceRegistry.getInstance('ConnectionManager'));
});

self.addEventListener('install', () => self.skipWaiting());

self.addEventListener('activate', () => {
    self.clients.matchAll({ type: 'window' }).then((windowClients) => {
        for (let client of windowClients as WindowClient[]) {
            // Force open pages to refresh, so that they have a chance to load the
            // fresh navigation response from the local dev server.
            client.navigate(client.url);
        }
    });
});

self.addEventListener('fetch', (event) => {
    const httpClient = serviceRegistry.getInstance('HttpClient');
    event.respondWith(httpClient.fetch(event.request));
});

self.addEventListener('message', async (event) => {
    const message: ServiceWorkerMessage = event.data;

    logger.info('onMessage', {
        type: message.type,
    });

    switch (message.type) {
        case authenticateMessageType: {
            const credentialsManager = serviceRegistry.getInstance('CredentialsManager');
            credentialsManager.setCredentials(message.payload.sessionId, {
                token: message.payload.token,
            });

            const connectionManager = serviceRegistry.getInstance('ConnectionManager');
            connectionManager.initializeConnection(message.payload);
            return;
        }
        case disconnectCloudEnv: {
            const connectionManager = serviceRegistry.getInstance('ConnectionManager');
            connectionManager.disposeConnection(message.payload);
            return;
        }
        case configureServiceWorker: {
            const configurationManager = serviceRegistry.getInstance('ConfigurationManager');
            if (
                configurationManager.configuration.features.useSharedConnection !==
                message.payload.features.useSharedConnection
            ) {
                serviceRegistry.unregisterInstance('LiveShareClient');
                serviceRegistry.unregisterInstance('ConnectionFactory');
                serviceRegistry.unregisterInstance('ConnectionManager');
                serviceRegistry.unregisterInstance('HttpClient');
            }
            configurationManager.updateConfiguration(message.payload);

            return;
        }
        case updateLiveShareConnectionInfo: {
            const configurationManager = serviceRegistry.getInstance('ConfigurationManager');
            if (!configurationManager.configuration.features.useSharedConnection) {
                throw new Error(
                    'Trying to use shared connection manager, but the feature is not enabled.'
                );
            }

            const liveShareClient = serviceRegistry.getInstance(
                'LiveShareClient'
            ) as InMemoryLiveShareClient;

            liveShareClient.setWorkspaceInfo(
                message.payload.sessionId,
                message.payload.workspaceInfo
            );
            liveShareClient.setWorkspaceAccess(
                message.payload.workspaceInfo.id,
                message.payload.workspaceAccess
            );

            return;
        }
    }
});
