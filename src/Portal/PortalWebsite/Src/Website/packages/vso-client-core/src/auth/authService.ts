import { IPartnerInfo } from '../interfaces/IPartnerInfo';
import { createKeys } from '../keychain/createKeys';
import { fetchKeychainKeys } from '../keychain/fetchKeychainKeys';
import { setKeychainKeys } from '../keychain/localstorageKeychainKeys';
import { localStorageKeychain } from '../keychain/localstorageKeychain';
import { validatePartnerInfo } from '../postMessageChannel/validatePartnerInfo';
import { createTrace } from '../utils/createTrace';
import { IKeychainKey } from '../interfaces/IKeychainKey';

const trace = createTrace('vso-client-core:authService');

export class AuthService {
    private keys: IKeychainKey[] = [];

    private getKeychainPartnerInfoKey = (environmentId: string) => {
        const key = `vso.partners.${environmentId}`;

        return key;
    };

    public storePartnerInfo = async (info: IPartnerInfo) => {
        const { token } = info;

        const keys = await createKeys(token);
        if (!keys || !keys.length) {
            trace.error(`Cannot create the encryption keys.`);
            throw new Error('Cannot get the encryption keys, is the Cascade token correct?');
        }

        this.keys = keys;

        setKeychainKeys(keys);

        const key = this.getKeychainPartnerInfoKey(info.environmentId);
        await localStorageKeychain.set(key, JSON.stringify(info));

        return info;
    };

    public removePartnerInfo = async (environmentId: string) => {
        const key = this.getKeychainPartnerInfoKey(environmentId);
        await localStorageKeychain.delete(key);
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

    public getCachedPartnerInfo = async (environmentId: string): Promise<IPartnerInfo | null> => {
        if (!this.keys.length) {
            const keys = await this.getKeychainKeys();
            if (!keys) {
                trace.info(`Cannot create the encryption keys.`);
                return null;
            }

            this.keys = keys;
        }

        const key = this.getKeychainPartnerInfoKey(environmentId);

        const infoString: string | undefined = await localStorageKeychain.get(key);
        if (!infoString) {
            return null;
        }

        try {
            const info = JSON.parse(infoString);
            validatePartnerInfo(info);

            return info as IPartnerInfo;
        } catch (e) {
            trace.error(e.message, e.stack);
        }

        return null;
    };
};

export const authService = new AuthService();
