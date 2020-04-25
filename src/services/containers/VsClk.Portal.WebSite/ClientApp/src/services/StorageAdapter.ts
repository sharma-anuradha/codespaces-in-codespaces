import * as msal from '@vs/msal';

import { localStorageKeychain } from 'vso-client-core';

class StorageAdapter extends msal.CustomStorage {
    private keyvalues: Map<string, string>;

    constructor() {
        super();

        // read unencrypted 
        this.keyvalues = new Map();
    }

    async init() {
        // read encrypted keys
        let storageAdapterValue = await localStorageKeychain.get('vso-storageadapter');
        if (storageAdapterValue) {
            this.keyvalues = (JSON.parse(storageAdapterValue) as any).reduce((m: any, [key, val]: any) => m.set(key, val), new Map());
        }
    }

    setItem(key: string, value: string) {
        this.keyvalues.set(key, value);
        localStorageKeychain.set('vso-storageadapter', JSON.stringify([...this.keyvalues.entries()]));
    }
    getItem(key: string) {
        return this.keyvalues.get(key) || '';
    }
    removeItem(key: string) {
        this.keyvalues.delete(key);
        localStorageKeychain.set('vso-storageadapter', JSON.stringify([...this.keyvalues.entries()]));
    }
    clear() {
        this.keyvalues.clear();
        localStorageKeychain.set('vso-storageadapter', JSON.stringify([...this.keyvalues.entries()]));
    }
    key(index: number) {
        return this.getAllKeys()[index];
    }
    getAllKeys() {
        let a = [];
        for (let key of this.keyvalues.keys()) {
            a.push(key);
        }
        return a;
    }
}

export const storageAdapter = new StorageAdapter();
