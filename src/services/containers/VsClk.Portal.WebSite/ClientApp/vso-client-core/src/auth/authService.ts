import { IPartnerInfo } from '../interfaces/IPartnerInfo';
import { createKeys } from '../keychain/createKeys';
import { setKeychainKeys } from '../keychain/localstorageKeychainKeys';
import { localStorageKeychain } from '../keychain/localstorageKeychain';
import { validatePartnerInfo } from '../postMessageChannel/validatePartnerInfo';
import { createTrace } from '../utils/createTrace';

const trace = createTrace('vso-client-core:authService');

export class AuthService {
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

        setKeychainKeys(keys);

        const key = this.getKeychainPartnerInfoKey(info.environmentId)
        await localStorageKeychain.set(key, JSON.stringify(info))

        return info;
    };

    public getCachedPartnerInfo = async (environmentId: string): Promise<IPartnerInfo | null> => {
        const key = this.getKeychainPartnerInfoKey(environmentId)
        const infoString: string | undefined = await localStorageKeychain.get(key);

        if (!infoString) {
            return null;
        }

        try {
            const info = JSON.parse(infoString);
            validatePartnerInfo(info);

            return info as IPartnerInfo;
        } catch {
            // no-op
        }
        
        return null;
    };
};

export const authService = new AuthService();
