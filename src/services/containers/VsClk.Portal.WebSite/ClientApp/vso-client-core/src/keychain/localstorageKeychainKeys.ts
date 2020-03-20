import { IKeychainKey } from '../interfaces/IKeychainKey';
import { randomBytes } from '../utils/randomBytes';

const randomKeyId = 'vso-random-keychain-key';

const getRandomKey = () => {
    const key = keychainKeys.find((key: IKeychainKey) => {
        return (key.id === randomKeyId);
    });

    return key;
}

const createRandomKeyExpirationTime = () => {
    return Date.now() + 24 * 60 * 60 * 1000;
}

export const removeRandomKey = () => {
    keychainKeys = keychainKeys.filter((key) => {
        return (key.id !== randomKeyId);
    });
}

const createInvalidKeyExpirationTime = () => {
    return Date.now() - 24 * 60 * 60 * 1000;
}

const invalidateRandomKey = () => {
    const currentRandomKey = getRandomKey();

    if (!currentRandomKey) {
        return;
    }

    if (currentRandomKey) {
        removeRandomKey();
    }

    keychainKeys.push({
        id: randomKeyId,
        key: currentRandomKey.key,
        expiresOn: createInvalidKeyExpirationTime(),
        method: 'AES',
        methodMode: 'CBC',
    });
}

export const defaultKey: IKeychainKey = {
    id: '012345827ccb0eea8a706c4c34a16891f84e7c',
    // An example 128-bit key
    key: new Buffer('0123456789ABCDEF'),
    // expired day ago
    expiresOn: createInvalidKeyExpirationTime(),
    method: 'AES',
    methodMode: 'CBC',
};

let keychainKeys: IKeychainKey[] = [];
export const getKeychainKeys = (): IKeychainKey[] => {
    return [
        { ...defaultKey },
        ...keychainKeys
    ];
}

export const setKeychainKeys = (keys: IKeychainKey[]): IKeychainKey[] => {
    const randomKey = getRandomKey();

    keychainKeys = [...keys];

    if (randomKey) {
        keychainKeys.push({ ...randomKey });
    }

    invalidateRandomKey();
    
    return keychainKeys;
}

/**
 * Function to add random key. If one already present, update its expiration time.
 */
export const addRandomKey = () => {
    const currentRandomKey = getRandomKey();

    let key = (currentRandomKey)
        ? currentRandomKey.key
        : randomBytes(16);

    if (currentRandomKey) {
        removeRandomKey();
    }

    keychainKeys.push({
        id: randomKeyId,
        key,
        expiresOn: createRandomKeyExpirationTime(),
        method: 'AES',
        methodMode: 'CBC',
    });
}

export const addDefaultGithubKey = () => {
    keychainKeys.push({
        id: 'github-keychain-key',
        key: new Buffer('ABCDEF0123456789'),
        expiresOn: Date.now() + (31 * 24 * 60 * 60 * 1000),
        method: 'AES',
        methodMode: 'CBC',
    });
}

/**
 * Function to add random key. If one already present, update its expiration time.
 */
export const isExpiredKey = (keyId: string) => {
    const key = keychainKeys.find((k) => {
        return k.id === keyId;
    });

    if (!key) {
        return true;
    }

    return (key.expiresOn <= Date.now());
}
