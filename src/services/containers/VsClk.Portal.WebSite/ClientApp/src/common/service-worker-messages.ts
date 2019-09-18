import { ConnectionDetails } from './connection-details';
import { ServiceWorkerConfiguration } from './service-worker-configuration';

export const authenticateMessageType = 'cloudenv/authenticate';
export type Connected = {
    type: typeof authenticateMessageType;
    payload: ConnectionDetails;
};

export const disconnectCloudEnv = 'cloudenv/disconnect';
export type Disconnect = {
    type: typeof disconnectCloudEnv;
    payload: {
        sessionId: string;
    };
};

export const configureServiceWorker = 'cloudenv/configure';
export type Configure = {
    type: typeof configureServiceWorker;
    payload: ServiceWorkerConfiguration;
};

export type ServiceWorkerMessage = Connected | Disconnect | Configure;
