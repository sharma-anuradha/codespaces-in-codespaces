import { enhanceEncryptionKeys } from './enhanceEncryptionKeys';
import { createTrace } from '../utils/createTrace';

const trace = createTrace('vso-client-core:create-encryption-keys');

export const createKeys = async (token: string) => {
    const result = await fetch('/keychain-keys', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            Authorization: `Bearer ${token}`,
        },
    });
    
    if (result.status !== 200) {
        trace.error(result);
        return [];
    }
    
    const keys = await result.json();
    return enhanceEncryptionKeys(keys);
};
