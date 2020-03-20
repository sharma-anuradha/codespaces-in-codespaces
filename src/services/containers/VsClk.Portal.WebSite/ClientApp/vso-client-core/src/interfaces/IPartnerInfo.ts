import { IGitCredential } from "./IGitCredential";
import { TPostMessageChannelMessages } from "./TPostMessageChannelMessages";

export interface IPartnerInfo {
    readonly type: TPostMessageChannelMessages.GetPartnerInfoResponse;
    readonly partnerName: 'github' | 'salesforce';
    readonly managementPortalUrl: string;
    readonly responseId: string;
    readonly environmentId: string;
    readonly token: string;
    readonly credentials: IGitCredential[];
};
