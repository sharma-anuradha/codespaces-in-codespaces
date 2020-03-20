import { randomBytes } from './randomBytes';

export const randomString = (length = 16, encoding = 'hex') => {
    const randomBuffer = randomBytes(length);

    return randomBuffer.toString(encoding);
}
