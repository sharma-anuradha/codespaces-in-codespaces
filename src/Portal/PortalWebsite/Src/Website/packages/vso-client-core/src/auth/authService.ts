import { createKeys } from '../keychain/createKeys';
import { fetchKeychainKeys } from '../keychain/fetchKeychainKeys';
import { setKeychainKeys } from '../keychain/localstorageKeychainKeys';
import { localStorageKeychain } from '../keychain/localstorageKeychain';
import { createTrace } from '../utils/createTrace';
import { IKeychainKey } from '../interfaces/IKeychainKey';
import { IPartnerInfo } from '../interfaces/IPartnerInfo';
import {
    validatePartnerInfoPostmessage,
    KNOWN_PARTNERS,
} from '../postMessageChannel/validatePartnerInfo';
import { PARTNER_INFO_KEYCHAIN_KEY } from '../constants';
import { VSCodespacesPlatformInfoGeneral } from 'vs-codespaces-authorization';

const trace = createTrace('vso-client-core:authService');

const validatePartnerInfo = (partnerInfo: IPartnerInfo | VSCodespacesPlatformInfoGeneral) => {
    if (!(partnerInfo as VSCodespacesPlatformInfoGeneral).codespaceToken) {
        return validatePartnerInfoPostmessage(partnerInfo as IPartnerInfo);
    } else {
        const info = partnerInfo as VSCodespacesPlatformInfoGeneral;

        if (!info.codespaceToken) {
            throw new Error('No `codespaceToken` set.');
        }

        if (!info.managementPortalUrl) {
            throw new Error('No `managementPortalUrl` set.');
        }

        if (!info.codespaceId) {
            throw new Error('No `codespaceId` set.');
        }

        if (!KNOWN_PARTNERS.includes(info.partnerName)) {
            throw new Error(`Unknown partner "${info.partnerName}".`);
        }
    }
};

export class AuthService {
    private keys: IKeychainKey[] = [];

    private getKeychainPartnerInfoKey = (environmentId: string) => {
        const key = `vso.partners.${environmentId}`;

        return key;
    };

    public getPartnerInfoToken = (info: IPartnerInfo | VSCodespacesPlatformInfoGeneral) => {
        const token = 'codespaceToken' in info ? info.codespaceToken : info.token;

        return token;
    };

    public storePartnerInfo = async (info: IPartnerInfo | VSCodespacesPlatformInfoGeneral) => {
        const token = this.getPartnerInfoToken(info);

        const keys = await createKeys(token);
        if (!keys || !keys.length) {
            trace.error(`Cannot create the encryption keys.`);
            throw new Error('Cannot get the encryption keys, is the Codespace token correct?');
        }

        this.keys = keys;

        setKeychainKeys(keys);

        const codespaceId = 'codespaceId' in info ? info.codespaceId : info.environmentId;

        const key = this.getKeychainPartnerInfoKey(codespaceId);
        await localStorageKeychain.set(key, JSON.stringify(info));

        return info;
    };

    public removePartnerInfo = async (environmentId: string) => {
        const key = this.getKeychainPartnerInfoKey(environmentId);
        await localStorageKeychain.delete(key);
        await localStorageKeychain.delete(PARTNER_INFO_KEYCHAIN_KEY);
    };

    public getKeychainKeys = async (): Promise<IKeychainKey[] | null> => {
        const keys = await fetchKeychainKeys();
        if (!keys || !keys.length) {
            trace.error(`Cannot create the encryption keys.`);
            return null;
        }

        setKeychainKeys(keys);

        return keys;
    };

    public getCachedPartnerInfo = async (
        environmentId: string
    ): Promise<IPartnerInfo | VSCodespacesPlatformInfoGeneral | null> => {
        if (!this.keys.length) {
            const keys = await this.getKeychainKeys();
            if (!keys) {
                trace.info(`Cannot create the encryption keys.`);
                return null;
            }

            this.keys = keys;
        }

        const partnerInfo = await this.getInfoForKey(environmentId);
        if (partnerInfo) {
            return partnerInfo;
        }

        const crossDomainPartnerInfo = await this.getInfoForKey(PARTNER_INFO_KEYCHAIN_KEY);

        return crossDomainPartnerInfo;
    };

    private async getInfoForKey(
        key: typeof PARTNER_INFO_KEYCHAIN_KEY
    ): Promise<VSCodespacesPlatformInfoGeneral | null>;
    private async getInfoForKey(key: string): Promise<IPartnerInfo | null>;
    private async getInfoForKey(key: any) {
        const keychainKey =
        key === PARTNER_INFO_KEYCHAIN_KEY
                ? PARTNER_INFO_KEYCHAIN_KEY
                : this.getKeychainPartnerInfoKey(key);

        const infoString: string | undefined = await localStorageKeychain.get(keychainKey);

        if (!infoString) {
            return null;
        }

        try {
            const info = JSON.parse(infoString);
            validatePartnerInfo(info);

            return info as IPartnerInfo | VSCodespacesPlatformInfoGeneral;
        } catch (e) {
            trace.error(e.message, e.stack);
        }

        return null;
    }
}

export const authService = new AuthService();
