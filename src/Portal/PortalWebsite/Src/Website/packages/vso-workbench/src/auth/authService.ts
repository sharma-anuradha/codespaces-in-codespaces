import JwtDecode from 'jwt-decode';
import { Emitter, Event } from 'vscode-jsonrpc';

import {
    authService as partnerAuthInfo,
    createTrace,
    debounceInterval,
    timeConstants,
    getCurrentEnvironmentId,
    tryGetCurrentEnvironmentId,
    PARTNER_INFO_KEYCHAIN_KEY,
} from 'vso-client-core';

import {
    VSCodespacesPlatformInfoGeneral,
    VSCodeDefaultAuthSession,
} from 'vs-codespaces-authorization';

import { FatalPlatformRedirectionError } from '../errors/FatalPlatformRedirectionError';
import { getPartnerLoginRedirectionURL } from '../utils/getPartnerLoginRedirectionURL';
import { isValidCascadeToken } from '../utils/isValidCascadeToken';
import { isJwtTokenWithMicrosoftEmail } from '../utils/isJwtTokenWithMicrosoftEmail';
import { AuthenticationError } from '../errors/AuthenticationError';
import { TAuthServiceEvent } from '../interfaces/TAuthServiceEvent';

const trace = createTrace('vso-workbench-auth-service');

export class AuthService {
    private isInternalUser = false;
    private eventsEventEmitter = new Emitter<TAuthServiceEvent>();

    public onEvent: Event<TAuthServiceEvent> = this.eventsEventEmitter.event;

    get isInternal() {
        return this.isInternalUser;
    }

    public getPartnerInfo = async () => {
        const codespaceId = tryGetCurrentEnvironmentId() || PARTNER_INFO_KEYCHAIN_KEY;
        const partnerInfo = await partnerAuthInfo.getCachedPartnerInfo(codespaceId);

        if (!partnerInfo) {
            return null;
        }

        return partnerInfo;
    };

    public getCachedGithubToken = async (): Promise<string | null> => {
        const partnerInfo = await this.getPartnerInfo();

        if (!partnerInfo) {
            return null;
        }

        const githubToken = partnerInfo.credentials.find((token) => {
            return token.host === 'github.com';
        });

        if (!githubToken) {
            return null;
        }

        return githubToken.token;
    };

    public getCachedCodespaceToken = async (): Promise<string | null> => {
        const partnerInfo = await this.getPartnerInfo();

        if (!partnerInfo) {
            return null;
        }

        if ('codespaceToken' in partnerInfo) {
            return partnerInfo.codespaceToken;
        }

        return partnerInfo.token;
    };

    public getCachedToken = async (): Promise<string | null> => {
        const partnerInfo = await this.getPartnerInfo();
        if (!partnerInfo) {
            throw new AuthenticationError('Cannot get the partner info.');
        }

        const token = partnerAuthInfo.getPartnerInfoToken(partnerInfo);
        if (!token) {
            return null;
        }

        this.setIsInternal(token);

        if (!isValidCascadeToken(token)) {
            return null;
        }

        return token;
    };

    public redirectToLogin = async () => {
        const partnerInfo = await this.getPartnerInfo();

        const redirectUrl = getPartnerLoginRedirectionURL(partnerInfo);
        if (!redirectUrl) {
            throw new FatalPlatformRedirectionError('Cannot get login redirection URL.');
        }

        const codespaceId =
            partnerInfo && 'codespaceId' in partnerInfo
                ? partnerInfo.codespaceId
                : tryGetCurrentEnvironmentId();

        if (codespaceId) {
            /**
             * For legacy reasons we have to support the old `environmentId` name,
             * the query param is only used by Saleforce at the moment.
             */
            redirectUrl.searchParams.append('environmentId', codespaceId);
            redirectUrl.searchParams.append('codespaceId', codespaceId);
        }

        redirectUrl.searchParams.append('url', location.href);

        location.href = redirectUrl.toString();
    };

    public signOut = async () => {
        const codespaceId = tryGetCurrentEnvironmentId() || PARTNER_INFO_KEYCHAIN_KEY;

        await partnerAuthInfo.removePartnerInfo(codespaceId);

        if (this.keepUserAuthenticated) {
            this.keepUserAuthenticated.stop();
        }

        this.eventsEventEmitter.fire('signed-out');
    };

    private setIsInternal = async (token: string) => {
        try {
            const jwtToken = JwtDecode(token);
            const isInternal = isJwtTokenWithMicrosoftEmail(jwtToken);

            this.isInternalUser = isInternal;
            trace.info(`Setting user isInternal flag to: ${this.isInternalUser}.`);
        } catch (e) {
            trace.info(`Failed to set isInternal flag.`, e);
        }
    };

    private makeTokenRequest = async () => {
        const keys = await partnerAuthInfo.getKeychainKeys();

        if (!keys) {
            await this.signOut();
        }
    };

    public keepUserAuthenticated = debounceInterval(
        this.makeTokenRequest,
        5 * timeConstants.MINUTE_MS
    );

    public getSettingsSyncSession = async (): Promise<VSCodeDefaultAuthSession | null> => {
        const info = await this.getPartnerInfo();
        if (!info || !('codespaceToken' in info)) {
            return null;
        }

        const { vscodeSettings } = info as VSCodespacesPlatformInfoGeneral;
        if (!vscodeSettings) {
            return null;
        }

        if (!('authenticationSessionId' in vscodeSettings)) {
            return null;
        }

        const { authenticationSessionId, defaultAuthSessions } = vscodeSettings;
        if (!defaultAuthSessions?.length) {
            return null;
        }

        const session = defaultAuthSessions.find((s) => {
            return s.id === authenticationSessionId;
        });

        if (!session) {
            return null;
        }

        return session;
    };
}

export const authService = new AuthService();
