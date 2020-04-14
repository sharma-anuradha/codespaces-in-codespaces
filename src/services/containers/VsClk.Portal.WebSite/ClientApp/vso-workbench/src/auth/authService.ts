import JwtDecode from 'jwt-decode';
import { Emitter, Event } from 'vscode-jsonrpc';

import {
    authService as partnerAuthInfo,
    createTrace,
    debounceInterval,
    timeConstants,
    getCurrentEnvironmentId,
} from 'vso-client-core';

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
        const partnerInfo = await partnerAuthInfo.getCachedPartnerInfo(getCurrentEnvironmentId());

        if (!partnerInfo) {
            return null;
        }

        return partnerInfo;
    };

    public getCachedToken = async (): Promise<string | null> => {
        const partnerInfo = await partnerAuthInfo.getCachedPartnerInfo(getCurrentEnvironmentId());
        if (!partnerInfo) {
            throw new AuthenticationError('Cannot get the partner info.');
        }

        const { token } = partnerInfo;
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

        redirectUrl.searchParams.append('environmentId', getCurrentEnvironmentId());

        location.href = redirectUrl.toString();
    };

    public signOut = async () => {
        await partnerAuthInfo.removePartnerInfo(getCurrentEnvironmentId());

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
}

export const authService = new AuthService();
