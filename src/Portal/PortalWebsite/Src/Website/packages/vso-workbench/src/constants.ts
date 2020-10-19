export const PLATFORM_REQUIRED_EXTENSIONS = ['vscode.theme-defaults', 'ms-vsonline.vsonline'];

export const DEFAULT_EXTENSIONS = [
    ...PLATFORM_REQUIRED_EXTENSIONS,
    'GitHub.vscode-pull-request-github',
];

export const DEFAULT_NON_ESSENTIAL_EXTENSIONS = [
    'ms-vsliveshare.vsliveshare',
    'visualstudioexptteam.vscodeintellicode',
];

export const HOSTED_IN_GITHUB_EXTENSIONS = ['github.github-vscode-theme'];

export const DEFAULT_GITHUB_VSCODE_AUTH_PROVIDER_ID = 'default-vso-github-vscode-auth-provider';

export const DEFAULT_MICROSFT_VSCODE_AUTH_PROVIDER_ID =
    'default-vso-microsoft-vscode-auth-provider';

export const DEFAULT_GITHUB_BROWSER_AUTH_PROVIDER_ID = 'default-vso-github-browser-auth-provider';

export const CONNECT_ATTEMPT_COUNT_LS_KEY = 'vscs-oauth-flow-attmept-count';

export enum PlatformQueryParams {
    // incoming / util params
    AutoStart = 'autoStart',
    AutoAuthRedirect = 'autoConnect',
    VSCodeChannel = 'vscodeChannel',
    // outgoing params
    CodespaceId = 'codespaceId',
}
