export {
    register as registerServiceWorker,
    onMessage
} from './serviceWorker';

export {
    authenticateMessageType,
    configureServiceWorker,
    connected,
    connectionFailed,
    disconnectCloudEnv,
    LiveShareConnectionInfo,
    ServiceWorkerMessage,
    tryAuthenticateMessageType,
    tryConfigureMessageType,
    updateLiveShareConnectionInfo,
} from './service-worker-messages';

export { postServiceWorkerMessage } from './post-message';

export { ServiceWorkerConfiguration } from './service-worker-configuration';

export type { IConnectionDetails } from './interfaces/IConnectionDetails';