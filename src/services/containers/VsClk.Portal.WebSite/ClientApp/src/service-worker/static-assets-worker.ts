import { LiveShareConnectionFactory } from './lib/connection-factory';
import { ConnectionManager } from './lib/connection-manager';
import { CredentialsManager } from './lib/credentials-manager';
import { LiveShareHttpClient } from './lib/http-client';
import { VSLS_API_URI } from '../constants';
import { createLogger } from './lib/logger';
import {
    ServiceWorkerMessage,
    authenticateMessageType,
    disconnectCloudEnv,
} from '../common/service-worker-messages';

declare var self: ServiceWorkerGlobalScope;

const connectionFactory = new LiveShareConnectionFactory(VSLS_API_URI);
const credentialsManager = new CredentialsManager();
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
                accessToken: message.payload.accessToken,
            });
            connectionManager.initializeConnection(message.payload);
            return;
        }
        case disconnectCloudEnv: {
            connectionManager.disposeConnection(message.payload);
            return;
        }
    }
});
