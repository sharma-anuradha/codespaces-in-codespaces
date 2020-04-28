import { IPackageJson } from './interfaces/IPackageJson';

const packageJson: IPackageJson = require('../package.json');

export const packageName = packageJson.name;

export const TELEMETRY_KEY = 'AIF-d9b70cd4-b9f9-4d70-929b-a071c400b217';

export const aadAuthorityUrlCommon = 'https://login.microsoftonline.com/common';

export const armAPIVersion = '2019-05-10';

export const expirationTimeBackgroundTokenRefreshThreshold = 3000;

export const blogPostUrl = 'https://aka.ms/vso-landing';

export const pricingInfoUrl = 'https://aka.ms/vso-pricing';

export const privacyStatementUrl = 'https://aka.ms/privacy';

export enum TerminalId {
    VMTerminal = 1,
    EnvironmentTerminal = 2,
}
