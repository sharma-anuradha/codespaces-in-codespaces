export const randomBytes = (length: number) => {
    if (!self.crypto) {
        throw new Error('No crypto API available.');
    }

    return self.crypto.getRandomValues(new Buffer(length));
};
