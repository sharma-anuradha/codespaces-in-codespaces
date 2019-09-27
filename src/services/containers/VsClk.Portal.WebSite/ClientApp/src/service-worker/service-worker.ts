import { ConfigurationManager } from './lib/configuration-manager';
import { ConnectionManager } from './lib/connection-manager';
import { CredentialsManager } from './lib/credentials-manager';
import { createLogger } from './lib/logger';
import { LiveShareConnectionFactory } from './lib/connection-factory';
import { LiveShareHttpClient } from './lib/http-client';

import {
    ServiceWorkerMessage,
    authenticateMessageType,
    disconnectCloudEnv,
    configureServiceWorker,
} from '../common/service-worker-messages';

declare var self: ServiceWorkerGlobalScope;

const credentialsManager = new CredentialsManager();
const configurationManager = new ConfigurationManager();
const connectionFactory = new LiveShareConnectionFactory(credentialsManager, configurationManager);
const connectionManager = new ConnectionManager(connectionFactory, credentialsManager);
const httpClient = new LiveShareHttpClient(connectionManager);

const logger = createLogger();

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
    event.respondWith(httpClient.fetch(event.request));
});

self.addEventListener('message', async (event) => {
    const message: ServiceWorkerMessage = event.data;

    logger.info('onMessage', {
        type: message.type,
    });

    switch (message.type) {
        case authenticateMessageType: {
            credentialsManager.setCredentials(message.payload.sessionId, {
                token: message.payload.token,
            });
            connectionManager.initializeConnection(message.payload);
            return;
        }
        case disconnectCloudEnv: {
            connectionManager.disposeConnection(message.payload);
            return;
        }
        case configureServiceWorker: {
            configurationManager.updateConfiguration(message.payload);
            return;
        }
    }
});
