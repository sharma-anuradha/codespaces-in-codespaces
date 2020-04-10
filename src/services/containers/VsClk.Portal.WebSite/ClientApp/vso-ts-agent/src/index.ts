export { createLogger } from './service-worker/lib/logger';
export { broadcast } from './service-worker/lib/post-message-utils';
export { VSLSWebSocket } from './VSLSWebSocket';
export { EnvConnector } from './envConnector';
export { assetsPathComponent, vscodeRemoteResourcePathComponent } from './service-worker/lib/url-utils';
export { postServiceWorkerMessage } from './service-worker/post-message';

export {
    register as registerServiceWorker,
    onMessage as onServiceWorkerMessage 
} from './service-worker/serviceWorker';

export {
    updateLiveShareConnectionInfo,
    tryAuthenticateMessageType,
    disconnectCloudEnv,
    ServiceWorkerMessage
} from './service-worker/service-worker-messages';

export { openSshChannel } from './openSshChannel';
export { WorkspaceClient } from './WorkspaceClient';

export { BrowserSyncService, BrowserConnectorMessages } from './services/BrowserSyncService';
export { GitCredentialService, GitCredentialsRequest } from './services/GitCredentialService';
