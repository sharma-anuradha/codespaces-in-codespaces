import { IPackageJson } from './interfaces/IPackageJson';
import { VSCodeQuality } from './utils/vscode';

export const DEFAULT_EXTENSIONS = [
    'vscode.theme-defaults',
    'ms-vsliveshare.vsliveshare',
    'visualstudioexptteam.vscodeintellicode',
    'ms-vsonline.vsonline',
];

const packageJson: IPackageJson = require('../package.json');

export interface VSCodeConfig {
    commit: string;
    quality: VSCodeQuality;
}

export const packageName = packageJson.name;

export function getVSCodeVersion(quality: VSCodeQuality): VSCodeConfig {
    return {
        commit: packageJson.vscodeCommit[quality],
        quality,
    };
}

export const TELEMETRY_KEY = 'AIF-d9b70cd4-b9f9-4d70-929b-a071c400b217';

export const aadAuthorityUrlCommon = 'https://login.microsoftonline.com/common';

export const armAPIVersion = '2019-05-10';

export const expirationTimeBackgroundTokenRefreshThreshold = 3000;

export const blogPostUrl = 'https://aka.ms/vso-landing';

export const pricingInfoUrl = 'https://aka.ms/vso-pricing';
