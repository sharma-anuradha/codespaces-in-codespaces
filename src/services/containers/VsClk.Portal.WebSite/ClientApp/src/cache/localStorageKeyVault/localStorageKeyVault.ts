import { Buffer } from 'buffer';

import { createTrace } from '../../utils/createTrace';
import { sendTelemetry } from '../../utils/telemetry';

import { encryptCipherPayload } from './encryptCipherPayload';
import { decryptCipherRecord } from './decryptCipherRecord';
import { IKeyVault } from '../../interfaces/IKeyVault';

import { ICipherRecord } from '../../interfaces/ICipherRecord';
import { IKeyVaultKey } from '../../interfaces/IKeyVaultKey';

export const trace = createTrace('LocalStorageKeyVault');

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
        } catch (error) {
            trace.error(error);
            sendTelemetry('vsonline/cipher/error', error);
        }
    }

    async delete(key: string) {
        try {
            localStorage.removeItem(key);
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
