import { Buffer } from 'buffer';

import { ICipherPayload } from '../interfaces/ICipherPayload';
import { IKeychainKey } from '../interfaces/IKeychainKey';
import { randomBytes } from '../utils/randomBytes';
import { intToBytes } from '../utils/intToBytes';
import { WebCipher } from './webCipher';
import { createTrace } from '../utils/createTrace';

export const trace = createTrace('LocalStorageKeychain:encrypt');

const MIN_PAD_LENGTH = 8;

export const encryptCipherPayload = async (unencryptedText: string, key: IKeychainKey): Promise<ICipherPayload | undefined> => {
    try {
        const ivBuffer = randomBytes(16);

        const cipher = new WebCipher(
            true,
            key.method,
            key.methodMode,
            key.key.length * 8,
            ivBuffer.length * 8
        );

        const startEncryptingTime = Date.now();

        const unencryptedBuffer = Buffer.from(unencryptedText, 'utf8');

        await cipher.init(key.key, ivBuffer);

        const header = intToBytes(unencryptedBuffer.length);

        const unencryptedBufferWithHeader = Buffer.concat([header, unencryptedBuffer]);
        
        // payload should be multiple of block length
        let padSize = cipher.blockLength - (unencryptedBufferWithHeader.length % cipher.blockLength);
        
        while (padSize < MIN_PAD_LENGTH) {
            padSize += cipher.blockLength;
        }

        const bufferToEncrypt = Buffer.concat([unencryptedBufferWithHeader, randomBytes(padSize)]);

        const payload = await cipher.transform(bufferToEncrypt);

        const record: ICipherPayload = {
            payload: payload.toString('base64'),
            iv: ivBuffer.toString('base64')
        }

        const encryptionDelta = (Date.now() - startEncryptingTime);

        trace.verbose('encryption took', encryptionDelta);

        return record;
    } catch (error) {
        trace.error(error);
        throw error;
    }
};
