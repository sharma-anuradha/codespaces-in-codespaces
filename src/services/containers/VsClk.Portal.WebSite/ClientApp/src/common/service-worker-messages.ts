import { ConnectionDetails } from './connection-details';

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

export type ServiceWorkerMessage = Connected | Disconnect;
