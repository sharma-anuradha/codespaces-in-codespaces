import deepEqual from 'deep-equal';

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
import { CriticalError } from './lib/errors/CriticalError';

declare var self: ServiceWorkerGlobalScope;

const logger = createLogger();

const serviceRegistry = new ServiceRegistry();

serviceRegistry.registerInstance('ConfigurationManager', new ConfigurationManager());
serviceRegistry.registerInstance('CredentialsManager', new CredentialsManager());
serviceRegistry.registerFactory('ConnectionFactory', (serviceRegistry) => {
    return new LiveShareConnectionFactory(serviceRegistry.getInstance('LiveShareClient'));
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

self.addEventListener('activate', (event) => {
    event.waitUntil(self.clients.claim());
});

self.addEventListener('error', (e) => {
    handleUnhandledError(e.error);
});

self.addEventListener('unhandledrejection', (ev: any) => {
    const event = ev as PromiseRejectionEvent;
    handleUnhandledError(event.reason);
});

async function handleUnhandledError(error: any) {
    // We don't want any error to unregister the service worker.
    if (error instanceof CriticalError) {
        await self.registration.unregister();

        logger.warn('Unregistered service worker. Updating clients.', error);

        const allClients = (await self.clients.matchAll({ type: 'window' })) as WindowClient[];
        for (const client of allClients) {
            // Reload only our top level (non-iframe) clients.
            if (client.frameType === 'top-level') {
                client.navigate(client.url);
            }
        }
    }
}

self.addEventListener('fetch', async (event) => {
    const httpClient = serviceRegistry.getInstance('HttpClient', false);
    event.respondWith(httpClient.fetch(event.request));
});

self.addEventListener('message', async (event) => {
    const message: ServiceWorkerMessage = event.data;

    logger.verbose('onMessage', {
        type: message.type,
    });

    switch (message.type) {
        case authenticateMessageType: {
            const credentialsManager = serviceRegistry.getInstance('CredentialsManager');
            credentialsManager.setCredentials(message.payload.sessionId, {
                token: message.payload.token,
            });

            // In case your client is telling to be in non-shared connection info mode,
            // switch to it.
            if (
                !serviceRegistry.canResolve('LiveShareClient') ||
                serviceRegistry.getInstance('LiveShareClient') instanceof InMemoryLiveShareClient
            ) {
                registerWebLiveShareClient();
            }

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
            const configuration = configurationManager.getConfigurationSafe();
            const newConfiguration = message.payload;

            const configurationChanged = deepEqual(configuration, newConfiguration);

            if (configurationChanged) {
                serviceRegistry.unregisterInstance('LiveShareClient');
                serviceRegistry.unregisterInstance('ConnectionFactory');
                serviceRegistry.unregisterInstance('ConnectionManager');
                serviceRegistry.unregisterInstance('HttpClient');

                configurationManager.updateConfiguration(newConfiguration);
            }

            if (newConfiguration.features.useSharedConnection) {
                registerInMemoryLiveShareClient();
            } else {
                registerWebLiveShareClient();
            }

            return;
        }
        case updateLiveShareConnectionInfo: {
            // In case your client is telling to be in shared connection info mode,
            // switch to it.
            if (
                !serviceRegistry.canResolve('LiveShareClient') ||
                serviceRegistry.getInstance('LiveShareClient') instanceof WebClient
            ) {
                registerInMemoryLiveShareClient();
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

function registerInMemoryLiveShareClient() {
    if (
        serviceRegistry.canResolve('LiveShareClient') &&
        serviceRegistry.getInstance('LiveShareClient') instanceof InMemoryLiveShareClient
    ) {
        return;
    }

    // We clean LiveShareClient and things that depend on it.
    if (
        serviceRegistry.canResolve('LiveShareClient') &&
        serviceRegistry.getInstance('LiveShareClient') instanceof WebClient
    ) {
        serviceRegistry.unregisterInstance('LiveShareClient');
        serviceRegistry.unregisterInstance('ConnectionFactory');
        serviceRegistry.unregisterInstance('ConnectionManager');
        serviceRegistry.unregisterInstance('HttpClient');
    }

    serviceRegistry.registerInstance('LiveShareClient', new InMemoryLiveShareClient());
}

function registerWebLiveShareClient() {
    if (
        serviceRegistry.canResolve('LiveShareClient') &&
        serviceRegistry.getInstance('LiveShareClient') instanceof InMemoryLiveShareClient
    ) {
        return;
    }

    // We clean LiveShareClient and things that depend on it.
    if (
        serviceRegistry.canResolve('LiveShareClient') &&
        serviceRegistry.getInstance('LiveShareClient') instanceof WebClient
    ) {
        serviceRegistry.unregisterInstance('LiveShareClient');
        serviceRegistry.unregisterInstance('ConnectionFactory');
        serviceRegistry.unregisterInstance('ConnectionManager');
        serviceRegistry.unregisterInstance('HttpClient');
    }

    const credentialsManager = serviceRegistry.getInstance('CredentialsManager');
    const configuration = serviceRegistry.getInstance('ConfigurationManager').configuration;
    serviceRegistry.registerInstance(
        'LiveShareClient',
        new WebClient(configuration.liveShareEndpoint, {
            getToken(sessionId: string) {
                const credentials = credentialsManager.getCredentials(sessionId);
                if (!credentials) {
                    throw new CriticalError('No credentials.');
                }
                return credentials.token;
            },
        })
    );

    serviceRegistry.registerInstance('LiveShareClient', new InMemoryLiveShareClient());
}
