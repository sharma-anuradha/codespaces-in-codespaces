import { ConnectionDetails } from './connection-details';
import { ServiceWorkerConfiguration } from './service-worker-configuration';
import { IWorkspaceInfo, IWorkspaceAccess } from '../ts-agent/client/ILiveShareClient';

export const authenticateMessageType = 'cloudenv/authenticate';
export type Authenticated = {
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
    payload: Partial<ServiceWorkerConfiguration>;
};

export const updateLiveShareConnectionInfo = 'cloudenv/update-liveshare-connection-info';
export type LiveShareConnectionInfo = {
    type: typeof updateLiveShareConnectionInfo;
    payload: {
        sessionId: string;
        workspaceInfo: IWorkspaceInfo;
        workspaceAccess: IWorkspaceAccess;
    };
};

export const connected = 'cloudenv/connected';
export type Connected = {
    type: typeof connected;
    payload: {
        sessionId: string;
    };
};
export const connectionFailed = 'cloudenv/connectionFailed';
export type ConnectionFailed = {
    type: typeof connectionFailed;
    payload: {
        sessionId: string;
    };
};

export type ServiceWorkerMessage =
    | Authenticated
    | Disconnect
    | Connected
    | ConnectionFailed
    | Configure
    | LiveShareConnectionInfo;
