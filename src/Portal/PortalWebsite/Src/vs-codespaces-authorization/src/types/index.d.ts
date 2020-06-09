import { VSCodespacesPlatformInfo } from './schema-base';
import { VSCodespacesPlatformInfo as VSCodespacesPlatformInfoInternal } from './schema-extended';

export function authorizePlatformInternal(endpoint: string, data: VSCodespacesPlatformInfoInternal): Promise<void>;
export function authorizePlatform(endpoint: string, data: VSCodespacesPlatformInfo): Promise<void>;

export { VSCodespacesPlatformInfo, VSCodespacesPlatformInfoInternal };
export type VSCodespacesPlatformInfoGeneral = VSCodespacesPlatformInfo | VSCodespacesPlatformInfoInternal;

export {
    GitCredentials,
    VSCodeExtension,
    VscodeSettings,
    VscodeChannel,
    VSCodeDefaultAuthSession,
    VSCodeHomeIndicator,
} from './schema-extended';
