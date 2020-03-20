import { Buffer } from 'buffer';

export const randomBytes = (length: number): Buffer => {
    if (!window.crypto) {
        throw new Error('No crypto API available.');
    }

    return window.crypto.getRandomValues(new Buffer(length));
};
