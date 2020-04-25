import { IKeychainKey } from '../interfaces/IKeychainKey';
import { randomBytes } from '../utils/randomBytes';

const randomKeyId = 'vso-random-keychain-key';

const defaultGitHubKey: IKeychainKey = {
    id: 'github-keychain-key',
    key: new Buffer('ABCDEF0123456789'),
    expiresOn: Date.now() + (31 * 24 * 60 * 60 * 1000),
    method: 'AES',
    methodMode: 'CBC',
};

export const addDefaultGithubKey = () => {
    removeKey(defaultGitHubKey.id);
    
    keychainKeys.push(defaultGitHubKey);
}

const getKey = (keyId: string) => {
    const key = keychainKeys.find((key: IKeychainKey) => {
        return (key.id === keyId);
    });

    return key;
}

const createRandomKeyExpirationTime = () => {
    return Date.now() + 24 * 60 * 60 * 1000;
}

export const removeKey = (keyId: string) => {
    keychainKeys = keychainKeys.filter((key) => {
        return (key.id !== keyId);
    });
}

const createInvalidKeyExpirationTime = () => {
    return Date.now() - 24 * 60 * 60 * 1000;
}

const invalidateKey = (keyId: string) => {
    const currentKey = getKey(keyId);

    if (!currentKey) {
        return;
    }

    if (currentKey) {
        removeKey(keyId);
    }

    keychainKeys.push({
        id: keyId,
        key: currentKey.key,
        expiresOn: createInvalidKeyExpirationTime(),
        method: 'AES',
        methodMode: 'CBC',
    });
}

export const invalidateGitHubKey = () => {
    return invalidateKey(defaultGitHubKey.id);
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
    const randomKey = getKey(randomKeyId);

    keychainKeys = [...keys];

    if (randomKey) {
        keychainKeys.push({ ...randomKey });
    }

    invalidateKey(randomKeyId);
    
    return keychainKeys;
}

/**
 * Function to add random key. If one already present, update its expiration time.
 */
export const addRandomKey = () => {
    const currentRandomKey = getKey(randomKeyId);

    let key = (currentRandomKey)
        ? currentRandomKey.key
        : randomBytes(16);

    if (currentRandomKey) {
        removeKey(randomKeyId);
    }

    keychainKeys.push({
        id: randomKeyId,
        key,
        expiresOn: createRandomKeyExpirationTime(),
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
