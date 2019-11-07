import { string } from 'prop-types';

export const INDEXEDDB_VSONLINE_DB = 'vsonline-web-db';
export const INDEXEDDB_LOGS_OBJECT_STORE = 'vsonline-logs-store';

export interface IAsyncStorage {
    getAllKeys(): Promise<string[]>;
    hasKey(key: string): Promise<boolean>;
    getValue(key: string): Promise<string>;
    setValue(key: string, value: string): Promise<void>;
    deleteKey(key: string): Promise<void>;
}

export class InMemoryAsyncStorage implements IAsyncStorage {
    private readonly store = new Map<string, string>();

    async getAllKeys(): Promise<string[]> {
        return Array.from(this.store.keys());
    }
    async hasKey(key: string): Promise<boolean> {
        return this.store.has(key);
    }
    async getValue(key: string): Promise<string> {
        return this.store.get(key) || '';
    }
    async setValue(key: string, value: string): Promise<void> {
        this.store.set(key, value);
    }
    async deleteKey(key: string): Promise<void> {
        this.store.delete(key);
    }
}

export class IndexedDBFS implements IAsyncStorage {
    private database!: IDBDatabase;

    async initialize() {
        this.database = await this.openDatabase(1);
    }

    private openDatabase(version: number): Promise<IDBDatabase> {
        return new Promise((resolve, reject) => {
            const request = window.indexedDB.open(INDEXEDDB_VSONLINE_DB, version);
            request.onerror = (err) => reject(request.error);
            request.onsuccess = () => {
                const db = request.result;
                if (db.objectStoreNames.contains(INDEXEDDB_LOGS_OBJECT_STORE)) {
                    resolve(db);
                }
            };
            request.onupgradeneeded = () => {
                const db = request.result;
                if (!db.objectStoreNames.contains(INDEXEDDB_LOGS_OBJECT_STORE)) {
                    db.createObjectStore(INDEXEDDB_LOGS_OBJECT_STORE);
                }
            };
        });
    }

    public async getAllKeys(): Promise<string[]> {
        return new Promise(async (resolve, reject) => {
            const objectStore = await this.getObjectStore();
            if (typeof objectStore.getAllKeys === 'function') {
                const request = objectStore.getAllKeys();
                request.onerror = () => reject(request.error);
                request.onsuccess = () => resolve(<string[]>request.result);
            } else {
                resolve([]);
            }
        });
    }

    public hasKey(key: string): Promise<boolean> {
        return new Promise<boolean>(async (resolve, reject) => {
            const objectStore = await this.getObjectStore();
            const request = objectStore.getKey(key);
            request.onerror = () => reject(request.error);
            request.onsuccess = () => {
                resolve(!!request.result);
            };
        });
    }

    public getValue(key: string): Promise<string> {
        return new Promise(async (resolve, reject) => {
            const objectStore = await this.getObjectStore();
            const request = objectStore.get(key);
            request.onerror = () => reject(request.error);
            request.onsuccess = () => resolve(request.result || '');
        });
    }

    public setValue(key: string, value: string): Promise<void> {
        return new Promise(async (resolve, reject) => {
            const objectStore = await this.getObjectStore('readwrite');
            const request = objectStore.put(value, key);
            request.onerror = () => reject(request.error);
            request.onsuccess = () => resolve();
        });
    }

    public deleteKey(key: string): Promise<void> {
        return new Promise(async (resolve, reject) => {
            const objectStore = await this.getObjectStore('readwrite');
            const request = objectStore.delete(key);
            request.onerror = () => reject(request.error);
            request.onsuccess = () => resolve();
        });
    }

    private async getObjectStore(
        option?: 'readonly' | 'readwrite' | 'versionchange' | undefined
    ): Promise<any> {
        const db = await this.database;
        let transaction;

        if (option) {
            transaction = db.transaction([INDEXEDDB_LOGS_OBJECT_STORE], option);
        } else {
            transaction = db.transaction([INDEXEDDB_LOGS_OBJECT_STORE]);
        }
        return transaction.objectStore(INDEXEDDB_LOGS_OBJECT_STORE);
    }
}

export async function deleteDatabase(name: string): Promise<void> {
    return new Promise((resolve, reject) => {
        const request = window.indexedDB.deleteDatabase(name);
        request.onerror = (err) => reject(request.error);
        request.onsuccess = () => {
            const db = request.result;
            if (!db) {
                resolve();
            } else {
                reject(request.error);
            }
        };
    });
}
