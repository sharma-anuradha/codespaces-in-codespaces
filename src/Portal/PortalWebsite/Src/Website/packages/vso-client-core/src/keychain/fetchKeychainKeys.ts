import { enhanceEncryptionKeys } from './enhanceEncryptionKeys';

export const fetchKeychainKeys = async () => {
    try {
        const result = await fetch('/keychain-keys', {
            method: 'GET',
            credentials: 'include',
        });

        if (!result.ok) {
            return null;
        }

        const keys = await result.json();
        return enhanceEncryptionKeys(keys);
    }
    catch (e) {
        // ignore
    }
    return null;
};
