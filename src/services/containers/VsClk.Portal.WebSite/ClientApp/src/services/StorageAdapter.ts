import * as msal from '@vs/msal';

import { localStorageKeyVault } from '../cache/localStorageKeyVaultInstance';

class StorageAdapter extends msal.CustomStorage {
    async setItem(key: string, value: string) {
        await localStorageKeyVault.set(key, value);
    }
    async getItem(key: string) {
        return await localStorageKeyVault.get(key) || '';
    }
    async removeItem(key: string) {
        await localStorageKeyVault.delete(key);
    }
    async clear() {
        await localStorageKeyVault.deleteAll();
    }
    async key(index: number) {
        const keys = localStorageKeyVault.getAllKeys();
        return keys[index];
    }
    async getAllKeys() {
        return localStorageKeyVault.getAllKeys();
    }
    *[Symbol.iterator]() {
        yield* localStorageKeyVault.getAllKeys();
    }
}

const defaultPropertyDescriptor = {
    enumerable: false,
    configurable: true,
};

export const storageAdapter = new Proxy(new StorageAdapter(), {
    ownKeys() {
        return localStorageKeyVault.getAllKeys();
    },
    getOwnPropertyDescriptor(target, key) {
        if (typeof key === 'symbol') {
            return defaultPropertyDescriptor;
        }
        const hasTheKey = localStorageKeyVault.has(key);
        if (!hasTheKey) {
            return defaultPropertyDescriptor;
        }
        return {
            ...defaultPropertyDescriptor,
            enumerable: true
        };
    },
    set(_, key) {
        throw new Error(`Cannot set the key "${key as string}" directly on the keyvault, please use "setItem" setter for the purpose.`);
    }
});
