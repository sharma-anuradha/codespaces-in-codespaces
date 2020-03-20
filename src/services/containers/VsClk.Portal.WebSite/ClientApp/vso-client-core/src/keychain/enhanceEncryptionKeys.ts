import { IKeychainKeyWithoutMethods, IKeychainKey } from '../interfaces/IKeychainKey';

export const enhanceEncryptionKeys = (keys: IKeychainKeyWithoutMethods[]): IKeychainKey[] => {
    const keysWithMethods = keys.map((key: IKeychainKeyWithoutMethods) => {
        return {
            id: key.id,
            key: new Buffer(key.key, 'base64'),
            expiresOn: parseInt(`${key.expiresOn}`, 10),
            method: 'AES',
            methodMode: 'CBC',
        } as IKeychainKey;
    });
    return keysWithMethods;
};
