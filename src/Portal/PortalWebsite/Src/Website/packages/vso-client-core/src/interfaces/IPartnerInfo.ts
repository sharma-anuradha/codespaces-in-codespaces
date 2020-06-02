import { IGitCredential } from './IGitCredential';
import { TPostMessageChannelMessages } from './TPostMessageChannelMessages';
import { IHomeIndicator, SessionData } from 'vscode-web';
import { TVSCodeQuality } from './TVSCodeQuality';

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

export interface INativeAuthProviderSession extends SessionData {
    type: TSupportedNativeVSCodeAuthProviders;
}

interface IVSCodeSettings {
    homeIndicator?: IHomeIndicator;
    /**
     * Settings to set if:
     *  1. No user settings is set in the browser storage (the first codespace run).
     *  2. No settings sync data for `settings` is present for the user (or settings sync service is turned of).
     */
    defaultSettings?: Record<string, any>;

    /**
     * Default extensions to preinstall if:
     *  1. No user settings is set in the browser storage (the first codespace run).
     *  2. No settings sync data for `extensions` is present for the user (or settings sync service is turned of).
     */
    defaultExtensions?: string[];

    /**
     * Enable Settings Sync by default. (default: false)
     */
    enableSyncByDefault?: boolean;
    
    /**
     * Provide the auth session id that will be used by default if `enableSyncByDefault`
     * is set to `true`. The session should come from appropriate item in the
     * `defaultAuthSessions` array.
     */
    authenticationSessionId?: string;

    /**
     * The array of default sessions used by the Native VSCode auth providers.
     */
    defaultAuthSessions?: INativeAuthProviderSession[];

    /**
     * VSCode bits quality: 'stable' | 'insider'
     */
    vscodeChannel?: TVSCodeQuality;
}

export interface ICrossDomainPartnerInfo {
    readonly partnerName: TKnownPartners;
    readonly cascadeToken: string;
    /**
     * Where to redirect to in case the credentials expire.
     */
    readonly managementPortalUrl: string;
    /**
     * Id of the codespace (guid)
     */
    readonly codespaceId: string;
    /**
     * Credentials for the git operations.
     */
    readonly credentials: IGitCredential[];
    /**
     * VSCode workbench settings.
     */
    readonly vscodeSettings: IVSCodeSettings;
};
