import { Buffer } from 'buffer';

export const randomBytes = (length: number): Buffer => {
    return window.crypto.getRandomValues(new Buffer(length));
}
