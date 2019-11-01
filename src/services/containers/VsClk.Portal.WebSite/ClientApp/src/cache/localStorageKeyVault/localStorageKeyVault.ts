import { Buffer } from 'buffer';

import { createTrace } from '../../utils/createTrace';
import { sendTelemetry } from '../../utils/telemetry';

import { encryptCipherPayload } from './encryptCipherPayload';
import { decryptCipherRecord } from './decryptCipherRecord';
import { IKeyVault } from '../../interfaces/IKeyVault';

import { ICipherRecord } from '../../interfaces/ICipherRecord';
import { IKeyVaultKey } from '../../interfaces/IKeyVaultKey';

export const trace = createTrace('LocalStorageKeyVault');

const ENTRIES_STORED_KEY = 'vsonline.keyvault.keys';

interface IStoredKeys {
    [key: string]: boolean;
}

export class LocalStorageKeyVault implements IKeyVault {
    constructor(
        private readonly keys: IKeyVaultKey[]
    ) {
        if (!this.keys || !this.keys.length) {
            const noKeysError = new Error('No keys provided.');

            sendTelemetry('vsonline/cipher/error', noKeysError);
            throw noKeysError;
        }

        // longer expiration times are in the beginning
        this.keys.sort((item: IKeyVaultKey) => {
            return -(item.expiresOn);
        });
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

    async get(key: string) {
        try {
            const storedValue = localStorage.getItem(key);

            if (!storedValue) {
                return storedValue;
            }

            const storedRecord: ICipherRecord = JSON.parse(storedValue);
            const aesKey = this.getKeyToDecrypt(storedRecord.keyId);

            // if no key to decrypt found,
            // we cannot decrypt the key
            if (!aesKey) {
                sendTelemetry('vsonline/cipher/no-decryption-key', {});
                return;
            }

            const decryptedValue = await decryptCipherRecord(
                storedRecord,
                aesKey
            );

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

            const payload = await encryptCipherPayload(
                value,
                encryptionKey
            );

            if (!payload) {
                return;
            }

            const cipherRecord: ICipherRecord = {
                ...payload,
                keyId: encryptionKey.id
            }

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

    private getKeyToDecrypt(keyId: string): IKeyVaultKey | undefined {
        const aesKey = this.keys.find((key: IKeyVaultKey) => {
            return (key.id === keyId);
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

    private isValidAesKey(key: IKeyVaultKey) {
        const delta = new Date(key.expiresOn).getTime() - Date.now();

        return (delta > 0);
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
}

export const localStorageKeyVaultFactory = () => {
    const record: IKeyVaultKey = {
        id: '012345827ccb0eea8a706c4c34a16891f84e7c',
        // An example 128-bit key
        key: new Buffer('0123456789ABCDEF'),
        expiresOn: Date.now() + 10 * 24 * 60 * 60 * 1000,
        method: 'AES',
        methodMode: 'CBC'
    };

    return new LocalStorageKeyVault([{ ...record, expiresOn: Date.now() }, record]);
};
