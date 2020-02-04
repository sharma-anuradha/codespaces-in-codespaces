import * as msal from '@vs/msal';

import { localStorageKeychain } from '../cache/localStorageKeychainInstance';

class StorageAdapter extends msal.CustomStorage {
    async setItem(key: string, value: string) {
        await localStorageKeychain.set(key, value);
    }
    async getItem(key: string) {
        return await localStorageKeychain.get(key) || '';
    }
    async removeItem(key: string) {
        await localStorageKeychain.delete(key);
    }
    async clear() {
        await localStorageKeychain.deleteAll();
    }
    async key(index: number) {
        const keys = localStorageKeychain.getAllKeys();
        return keys[index];
    }
    async getAllKeys() {
        return localStorageKeychain.getAllKeys();
    }
    *[Symbol.iterator]() {
        yield* localStorageKeychain.getAllKeys();
    }
}

const defaultPropertyDescriptor = {
    enumerable: false,
    configurable: true,
};

export const storageAdapter = new Proxy(new StorageAdapter(), {
    ownKeys() {
        return localStorageKeychain.getAllKeys();
    },
    getOwnPropertyDescriptor(target, key) {
        if (typeof key === 'symbol') {
            return defaultPropertyDescriptor;
        }
        const hasTheKey = localStorageKeychain.has(key);
        if (!hasTheKey) {
            return defaultPropertyDescriptor;
        }
        return {
            ...defaultPropertyDescriptor,
            enumerable: true
        };
    },
    set(_, key) {
        throw new Error(`Cannot set the key "${key as string}" directly on the keychain, please use "setItem" setter for the purpose.`);
    }
});
