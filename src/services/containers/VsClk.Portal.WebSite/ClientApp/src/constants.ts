export const DEFAULT_EXTENSIONS = [
    'vscode.theme-defaults',
    'ms-vsliveshare.vsliveshare',
    'visualstudioexptteam.vscodeintellicode',
    '/.cloudenv/extensions/cloudenv-extensions.vsix',
    '/.cloudenv/extensions/cloudenv.vsix'
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

const VSLS_PROD_API_URI = 'https://prod.liveshare.vsengsaas.visualstudio.com';
const VSLS_DEV_API_URI = '/vsls-api';

export const VSLS_API_URI =
    process.env.NODE_ENV === 'production' ? VSLS_PROD_API_URI : VSLS_DEV_API_URI;

export const TELEMETRY_KEY = 'AIF-d9b70cd4-b9f9-4d70-929b-a071c400b217';
