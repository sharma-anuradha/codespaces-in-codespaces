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

export interface ICrossDomainPartnerInfo {
    readonly partnerName: TKnownPartners;
    readonly cascadeToken: string;
    readonly managementPortalUrl: string;
    readonly codespaceId: string;
    readonly credentials: IGitCredential[];
};
