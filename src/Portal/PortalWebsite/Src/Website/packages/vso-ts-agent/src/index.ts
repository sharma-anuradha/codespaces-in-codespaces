export { openSshChannel } from './openSshChannel';
export { WorkspaceClient } from './WorkspaceClient';
export { LiveShareWebClient } from './liveShareWebClient';
export { SshChannelOpenner } from './sshChannelOpenner';

export { BrowserSyncService, BrowserConnectorMessages } from './services/BrowserSyncService';
export { GitCredentialService, GitCredentialsRequest } from './services/GitCredentialService';

export { SupportedGitService } from './interfaces/SupportedGitService';

export { getSupportedGitServiceByHost } from './utils/getSupportedGitServiceByHost';
export { getSupportedGitService } from './utils/getSupportedGitService';