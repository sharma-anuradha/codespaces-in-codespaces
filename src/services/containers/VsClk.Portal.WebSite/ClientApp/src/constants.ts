export const DEFAULT_EXTENSIONS = [
    'vscode.theme-defaults',
    'ms-vsliveshare.vsliveshare',
    'visualstudioexptteam.vscodeintellicode',
    '/.vsonline/extensions/vsonline-extensions.vsix',
    '/.vsonline/extensions/vsonline.vsix',
];

const packageJson: { name: string; vscodeCommit: string } = require('../package.json');

export interface VSCodeConfig {
    commit: string;
    quality: 'stable' | 'insider';
}

export const packageName = packageJson.name;
export const vscodeConfig: VSCodeConfig = {
    commit: packageJson.vscodeCommit,
    quality: 'insider',
};

export interface IPackageJson {
    version: string;
}

export const TELEMETRY_KEY = 'AIF-d9b70cd4-b9f9-4d70-929b-a071c400b217';

export const aadAuthorityUrl = 'https://login.microsoftonline.com/organizations';

export const expirationTimeBackgroundTokenRefreshThreshold = 1800;
