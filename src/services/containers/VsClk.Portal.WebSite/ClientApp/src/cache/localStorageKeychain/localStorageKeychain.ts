
import { createTrace } from '../../utils/createTrace';
import { sendTelemetry } from '../../utils/telemetry';

import { encryptCipherPayload } from './encryptCipherPayload';
import { decryptCipherRecord } from './decryptCipherRecord';
import { IKeychain } from '../../interfaces/IKeychain';

import { ICipherRecord } from '../../interfaces/ICipherRecord';
import { IKeychainKey } from '../../interfaces/IKeychainKey';

import { getKeychainKeys, defaultKey, isExpiredKey } from './localstorageKeychainKeys';

export const trace = createTrace('LocalStorageKeychain');

const ENTRIES_STORED_KEY = 'vsonline.keychain.keys';

interface IStoredKeys {
    [key: string]: boolean;
}

export class LocalStorageKeychain implements IKeychain {
    private get keys() {
        const keys = getKeychainKeys()
                    .sort((a, b) => b.expiresOn - a.expiresOn);

        return keys;
    }

    private readBookKeepingKeys(): IStoredKeys {
        try {
            const storedKeys = localStorage.getItem(ENTRIES_STORED_KEY);

            if (!storedKeys) {
                return {};
            }

            const keys = JSON.parse(storedKeys);
            return keys || {};
        } catch {
            return {};
        }
    }

    private writeBookKeeingKeys(keys: IStoredKeys) {
        try {
            localStorage.setItem(ENTRIES_STORED_KEY, JSON.stringify(keys));
        } catch (e) {
            // ignore
        }
    }

    private bookkeepKey(key: string) {
        const keys = this.readBookKeepingKeys();

        keys[key] = true;

        this.writeBookKeeingKeys(keys);
    }

    private removeBookkeepKey(key: string) {
        const keys = this.readBookKeepingKeys();

        delete keys[key];

        this.writeBookKeeingKeys(keys);
    }

    private getCipherRecord(key: string): ICipherRecord | undefined {
        try {
            const storedValue = localStorage.getItem(key);

            if (!storedValue) {
                return;
            }

            const storedRecord: ICipherRecord = JSON.parse(storedValue);

            return storedRecord;
        } catch (e) {
            trace.error(e);
        }
    }

    async get(key: string) {
        try {
            const storedRecord = this.getCipherRecord(key);
            if (!storedRecord) {
                return;
            }

            const aesKey = this.getKeyToDecrypt(storedRecord.keyId);

            // if no key to decrypt found,
            // we cannot decrypt the key
            if (!aesKey) {
                sendTelemetry('vsonline/cipher/no-decryption-key', {});
                return;
            }

            const decryptedValue = await decryptCipherRecord(storedRecord, aesKey);

            if (!decryptedValue) {
                return;
            }

            return decryptedValue;
        } catch (error) {
            trace.error(error);
            sendTelemetry('vsonline/cipher/error', error);
        }
    }

    async set(keyToSet: string, value: string) {
        try {
            const encryptionKey = this.getKeyToEncrypt();

            const payload = await encryptCipherPayload(value, encryptionKey);

            if (!payload) {
                return;
            }

            const cipherRecord: ICipherRecord = {
                ...payload,
                keyId: encryptionKey.id,
            };

            const encryptedTextToSet = JSON.stringify(cipherRecord);

            localStorage.setItem(keyToSet, encryptedTextToSet);
            this.bookkeepKey(keyToSet);
        } catch (error) {
            trace.error(error);
            sendTelemetry('vsonline/cipher/error', error);
        }
    }

    async delete(key: string) {
        try {
            localStorage.removeItem(key);
            this.removeBookkeepKey(key);
        } catch (error) {
            trace.error(error);
            sendTelemetry('vsonline/cipher/error', error);
        }
    }

    private getKeyToDecrypt(keyId: string): IKeychainKey | undefined {
        const keys = this.keys;

        const aesKey = keys.find((key: IKeychainKey) => {
            return key.id === keyId;
        });

        return aesKey;
    }

    private getKeyToEncrypt() {
        // get the most recent one
        if (this.isValidAesKey(this.keys[0])) {
            return this.keys[0];
        }

        const noKeysError = new Error('No valid keys found.');

        sendTelemetry('vsonline/cipher/error', noKeysError);

        throw noKeysError;
    }

    private isValidAesKey(key: IKeychainKey) {
        const delta = new Date(key.expiresOn).getTime() - Date.now();

        return delta > 0;
    }

    public getAllKeys() {
        const keys = this.readBookKeepingKeys();

        if (!keys) {
            return [];
        }

        return Object.keys(keys);
    }

    public async deleteAll() {
        const keysObject = this.readBookKeepingKeys();

        const keys = Object.keys(keysObject);

        for (let key of keys) {
            await this.delete(key);
        }
    }

    public has(key: string | number): boolean {
        const keys = this.readBookKeepingKeys();

        return !!keys[key];
    }

    private async rehashKey(key: string) {
        const value = await this.get(key);

        if (value) {
            await this.set(key, value);
            return;
        }

        await this.delete(key);
    }
    
    public async rehash(rehashLegacyKeysOnly = false) {
        const keys = this.getAllKeys();

        for (let key of keys) {
            if (!rehashLegacyKeysOnly) {
                await this.rehashKey(key);
                continue;
            }

            const storedRecord = this.getCipherRecord(key);
            if (!storedRecord) {
                continue;
            }

            if (storedRecord.keyId === defaultKey.id || isExpiredKey(storedRecord.keyId)) {
                await this.rehashKey(key);
            }
        }
    }
}

export const localStorageKeychainFactory = () => {
    return new LocalStorageKeychain();
};
