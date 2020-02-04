import { Buffer } from 'buffer';

import { sendTelemetry } from '../../utils/telemetry';
import { createTrace } from '../../utils/createTrace';
import { WebCipher } from './webCipher';
import { ICipherPayload } from '../../interfaces/ICipherPayload';
import { IKeychainKey } from '../../interfaces/IKeychainKey';
import { bufferToInt } from '../../utils/bufferToInt';

export const trace = createTrace('LocalStorageKeychain:decrypt');

export const decryptCipherRecord = async (cipherRecord: ICipherPayload, key: IKeychainKey): Promise<string | undefined> => {
    try {
        const startDecryptingTimes = Date.now();

        const ivBuffer = Buffer.from(cipherRecord.iv, 'base64');

        const cipher = new WebCipher(
            false,
            key.method,
            key.methodMode,
            key.key.length * 8,
            ivBuffer.length * 8
        );

        await cipher.init(key.key, ivBuffer);

        const encryptedBuffer = Buffer.from(cipherRecord.payload, 'base64');
        const decryptedBuffer = await cipher.transform(encryptedBuffer);

        const playloadLengthBuffer = decryptedBuffer.slice(0, 4);
        const payloadLength = bufferToInt(playloadLengthBuffer);

        const payloadWithHeader = decryptedBuffer.slice(4);
        const payload = payloadWithHeader.slice(0, payloadLength);

        const decryptedText = payload.toString('utf8');

        const decryptionDelta = Date.now() - startDecryptingTimes;
        trace.verbose('decryption took', decryptionDelta);

        sendTelemetry('vsonline/cipher/decrypt', {
            timeSpent: decryptionDelta,
            payloadLengthBefore: cipherRecord.payload.length,
            payloadLengthAfter: decryptedText.length    
        });
        
        return decryptedText;
    } catch (error) {
        trace.error(error);
        sendTelemetry('vsonline/cipher/error', error);
        throw error;
    }
}
