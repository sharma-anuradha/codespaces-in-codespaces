import deepEqual from 'deep-equal';

import {
    authenticateMessageType,
    ServiceWorkerMessage,
    tryConfigureMessageType,
    configureServiceWorker,
    disconnectCloudEnv,
    updateLiveShareConnectionInfo,
    LiveShareConnectionInfo,
} from './service-worker-messages';
import { getRoutingDetails, allowRequestsForEnvironment } from './lib/url-utils';
import { ServiceWorkerConfiguration } from './service-worker-configuration';

import { createLogger } from './lib/logger';
import { ServiceRegistry } from './lib/service-registry';
import { ConfigurationManager } from './lib/configuration-manager';
import { CredentialsManager } from './lib/credentials-manager';
import { LiveShareConnectionFactory } from './lib/connection-factory';
import { ConnectionManager } from './lib/connection-manager';
import { PassThroughHttpClient, LiveShareHttpClient } from './lib/http-client';
import { CriticalError } from './lib/errors/CriticalError';
import { postMessage } from './lib/post-message-utils';
import { isDefined } from 'vso-client-core';
import { InMemoryLiveShareClient } from '../inMemoryClient';
import { LiveShareWebClient } from '../liveShareWebClient';

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

let configurationUpdateRequest: Promise<void> | undefined = undefined;
self.addEventListener('fetch', async (event) => {
    event.respondWith(
        (async () => {
            const configurationManager = serviceRegistry.getInstance('ConfigurationManager');
            const configuration = configurationManager.getConfigurationSync();
            if (
                !isDefined(configurationUpdateRequest) &&
                !isDefined(configuration) &&
                isDefined(getRoutingDetails(event.request.url))
            ) {
                configurationUpdateRequest = requestConfiguration(
                    event.clientId,
                    configurationManager
                );
            }

            if (configurationUpdateRequest) {
                await configurationUpdateRequest;
                configurationUpdateRequest = undefined;
            }

            const httpClient = serviceRegistry.getInstance('HttpClient');
            return httpClient.fetch(event.request);
        })()
    );
});

async function requestConfiguration(clientId: string, configurationManager: ConfigurationManager) {
    await postMessage(clientId, {
        type: tryConfigureMessageType,
    });

    try {
        const newConfiguration = await configurationManager.getConfiguration();
        await updateConfiguration(newConfiguration);
    } catch (err) {
        logger.error('Failed to get configuration.');

        throw new CriticalError('Failed to get configuration in time.');
    }
}

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

            if (message.payload.environmentId) {
                allowRequestsForEnvironment(
                    message.payload.environmentId,
                    message.payload.sessionId
                );
            }

            // In case your client is telling to be in non-shared connection info mode,
            // switch to it.
            if (
                !serviceRegistry.canResolve('LiveShareClient') ||
                serviceRegistry.getInstance('LiveShareClient') instanceof InMemoryLiveShareClient
            ) {
                await registerWebLiveShareClient();
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
            await updateConfiguration(message.payload);

            return;
        }
        case updateLiveShareConnectionInfo: {
            // In case your client is telling to be in shared connection info mode,
            // switch to it.
            if (
                !serviceRegistry.canResolve('LiveShareClient') ||
                serviceRegistry.getInstance('LiveShareClient') instanceof LiveShareWebClient
            ) {
                registerInMemoryLiveShareClient();
            }
            updateLiveShareConnection(message);

            return;
        }
    }
});

async function updateConfiguration(configuration: ServiceWorkerConfiguration) {
    const configurationManager = serviceRegistry.getInstance('ConfigurationManager');

    if (deepEqual(configuration, configurationManager.getConfigurationSync())) {
        return;
    }

    configurationManager.updateConfiguration(configuration);

    serviceRegistry.unregisterInstance('LiveShareClient');
    serviceRegistry.unregisterInstance('ConnectionFactory');
    serviceRegistry.unregisterInstance('ConnectionManager');
    serviceRegistry.unregisterInstance('HttpClient');

    if (configuration.features.useSharedConnection) {
        registerInMemoryLiveShareClient();
    } else {
        await registerWebLiveShareClient();
    }
}

function updateLiveShareConnection(message: LiveShareConnectionInfo) {
    const liveShareClient = serviceRegistry.getInstance(
        'LiveShareClient'
    ) as InMemoryLiveShareClient;

    liveShareClient.setWorkspaceInfo(message.payload.sessionId, message.payload.workspaceInfo);
    liveShareClient.setWorkspaceAccess(
        message.payload.workspaceInfo.id,
        message.payload.workspaceAccess
    );
}

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
        serviceRegistry.getInstance('LiveShareClient') instanceof LiveShareWebClient
    ) {
        serviceRegistry.unregisterInstance('LiveShareClient');
        serviceRegistry.unregisterInstance('ConnectionFactory');
        serviceRegistry.unregisterInstance('ConnectionManager');
        serviceRegistry.unregisterInstance('HttpClient');
    }

    serviceRegistry.registerInstance('LiveShareClient', new InMemoryLiveShareClient());
}

async function registerWebLiveShareClient() {
    if (
        serviceRegistry.canResolve('LiveShareClient') &&
        serviceRegistry.getInstance('LiveShareClient') instanceof LiveShareWebClient
    ) {
        return;
    }

    // We clean LiveShareClient and things that depend on it.
    if (
        serviceRegistry.canResolve('LiveShareClient') &&
        serviceRegistry.getInstance('LiveShareClient') instanceof InMemoryLiveShareClient
    ) {
        serviceRegistry.unregisterInstance('LiveShareClient');
        serviceRegistry.unregisterInstance('ConnectionFactory');
        serviceRegistry.unregisterInstance('ConnectionManager');
        serviceRegistry.unregisterInstance('HttpClient');
    }

    const credentialsManager = serviceRegistry.getInstance('CredentialsManager');
    const configuration = await serviceRegistry
        .getInstance('ConfigurationManager')
        .getConfiguration();

    serviceRegistry.registerInstance(
        'LiveShareClient',
        new LiveShareWebClient(configuration.liveShareEndpoint, {
            getToken(sessionId: string) {
                const credentials = credentialsManager.getCredentials(sessionId);
                if (!credentials) {
                    throw new CriticalError('No credentials.');
                }
                return credentials.token;
            },
        })
    );
}
