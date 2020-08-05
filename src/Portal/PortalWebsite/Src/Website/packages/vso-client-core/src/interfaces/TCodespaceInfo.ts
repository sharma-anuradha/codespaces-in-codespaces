import {
    VSCodespacesPlatformInfo,
    VSCodespacesPlatformInfoInternal,
} from 'vs-codespaces-authorization';
import { IPartnerInfo } from './IPartnerInfo';

// the union type of old and new Codespace info payload
export type TCodespaceInfo =
    | IPartnerInfo
    | VSCodespacesPlatformInfo
    | VSCodespacesPlatformInfoInternal;
