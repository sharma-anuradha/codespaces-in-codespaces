export { vscode } from './vscode/vscodeAssets/vscode';

export { UserDataProvider } from './vscode/providers/userDataProvider/userDataProvider';
export { WorkspaceProvider } from './vscode/providers/workspaceProvider/workspaceProvider';
export { UrlCallbackProvider } from './vscode/providers/userDataProvider/urlCallbackProvider';
export { resourceUriProviderFactory } from './vscode/providers/resourceUriProvider/resourceUriProviderFactory';
export { applicationLinksProviderFactory } from './vscode/providers/applicationLinksProvider/applicationLinksProviderFactory';
export { CredentialsProvider } from './vscode/providers/credentialsProvider/credentialsProvider';
export {
    BaseExternalUriProvider,
    PortForwardingExternalUriProvider,
} from './vscode/providers/externalUriProvider/externalUriProvider';

export { getVSCodeVersion, getVSCodeVersionString } from './utils/getVSCodeVersion';
export { getVSCodeAssetPath } from './utils/getVSCodeAssetPath';
export { getUriAuthority } from './utils/getUriAuthority';

export { IAuthStrategy } from './interfaces/IAuthStrategy';

export { MsalAuthStrategy } from './vscode/providers/credentialsProvider/strategies/MsalAuthStrategy';
export { AADv2BrowserSyncStrategy } from './vscode/providers/credentialsProvider/strategies/AADv2BrowserSyncStrategy';
export { LiveShareWebStrategy } from './vscode/providers/credentialsProvider/strategies/LiveShareWebStrategy';
export { LiveShareGithubAuthStrategy } from './vscode/providers/credentialsProvider/strategies/CascadeAuthStrategy';

export { CrossDomainPFAuthenticator } from './auth/portForwarding/CrossDomainPFAuthenticator';

export { getExtensions } from './vscode/workbenches/getExtensions';

export {
    DEFAULT_GITHUB_VSCODE_AUTH_PROVIDER_ID,
    DEFAULT_MICROSFT_VSCODE_AUTH_PROVIDER_ID
} from './constants';
