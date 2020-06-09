import {
    authService as vsoPartnerInfoService,
    timeConstants,
    IGitCredential,
    getCurrentEnvironmentId,
} from 'vso-client-core';

import { IAuthStrategy } from '../../../../interfaces/IAuthStrategy';

const isExpiredCredential = (credential: IGitCredential) => {
    const { expiration } = credential;

    if (typeof expiration !== 'number') {
        return false;
    }

    const delta = expiration - Date.now();
    return delta <= timeConstants.HOUR_MS;
};

const findCredential = async (service: string, account: string): Promise<string | null> => {
    if (!account || !service) {
        return null;
    }

    const info = await vsoPartnerInfoService.getCachedPartnerInfo(getCurrentEnvironmentId());
    if (!info) {
        return null;
    }

    const { credentials } = info;
    if (!credentials || !credentials.length) {
        return null;
    }

    const credential = credentials.find((c) => {
        const isServiceMatch = c.host === service;
        if (!isServiceMatch) {
            return null;
        }

        const isExpired = isExpiredCredential(c as IGitCredential);
        const isPath = account === '*' || account === c.path;

        if (isPath && !isExpired) {
            return c;
        }
    });

    if (!credential) {
        return null;
    }

    return credential.token;
};

/**
 * Strategy for `Git Credential Helper`[https://git-scm.com/docs/git-credential],
 * to pipe thru the credentials we might need for the partners.
 */
export class GitCredentialHelperStrategy implements IAuthStrategy {
    async canHandleService(service: string, account: string) {
        const result = await findCredential(service, account);

        return !!result;
    }

    async getToken(service: string, account: string): Promise<string | null> {
        const result = await findCredential(service, account);

        return result;
    }
}
