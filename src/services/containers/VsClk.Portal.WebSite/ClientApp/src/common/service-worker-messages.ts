import { ConnectionDetails } from './connection-details';
import { ServiceWorkerConfiguration } from './service-worker-configuration';
import { IWorkspaceInfo, IWorkspaceAccess } from '../ts-agent/client/ILiveShareClient';
import { isDefined } from '../utils/isDefined';

export const authenticateMessageType = 'vsonline/authenticate';
export type Authenticated = {
    type: typeof authenticateMessageType;
    payload: ConnectionDetails;
};

export const disconnectCloudEnv = 'vsonline/disconnect';
export type Disconnect = {
    type: typeof disconnectCloudEnv;
    payload: {
        sessionId: string;
    };
};

export const configureServiceWorker = 'vsonline/configure';
export type Configure = {
    type: typeof configureServiceWorker;
    payload: ServiceWorkerConfiguration;
};

export const updateLiveShareConnectionInfo = 'vsonline/update-liveshare-connection-info';
export type LiveShareConnectionInfo = {
    type: typeof updateLiveShareConnectionInfo;
    payload: {
        sessionId: string;
        workspaceInfo: IWorkspaceInfo;
        workspaceAccess: IWorkspaceAccess;
    };
};

export const connected = 'vsonline/connected';
export type Connected = {
    type: typeof connected;
    payload: {
        sessionId: string;
    };
};
export const connectionFailed = 'vsonline/connectionFailed';
export type ConnectionFailed = {
    type: typeof connectionFailed;
    payload: {
        sessionId: string;
    };
};

export const tryAuthenticateMessageType = 'vsonline/tryAuthenticate';
export type TryAuthenticate = {
    type: typeof tryAuthenticateMessageType;
};

export const tryConfigureMessageType = 'vsonline/tryConfigure';
export type TryConfigure = {
    type: typeof tryConfigureMessageType;
};

export type ServiceWorkerMessage =
    | TryAuthenticate
    | TryConfigure
    | Authenticated
    | Disconnect
    | Connected
    | ConnectionFailed
    | Configure
    | LiveShareConnectionInfo;

export function isServiceWorkerMessage(data: any): data is ServiceWorkerMessage {
    return isDefined(data.type);
}
