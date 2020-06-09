import { IGitCredential } from './IGitCredential';
import { TPostMessageChannelMessages } from './TPostMessageChannelMessages';

type TKnownPartners = 'github' | 'salesforce';

export interface IPartnerInfo {
    readonly type: TPostMessageChannelMessages.GetPartnerInfoResponse;
    readonly partnerName: TKnownPartners;
    readonly managementPortalUrl: string;
    readonly responseId: string;
    readonly environmentId: string;
    readonly token: string;
    readonly credentials: IGitCredential[];
};

export type TSupportedNativeVSCodeAuthProviders = 'github' | 'microsoft';
