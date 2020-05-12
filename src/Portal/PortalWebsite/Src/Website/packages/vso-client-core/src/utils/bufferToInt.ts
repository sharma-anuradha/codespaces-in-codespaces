export const bufferToInt = (buffer: Buffer) => {
    let result = 0;
    const start = buffer.length - 1;
    for (let i = start; i >= 0; i--) {
        result += buffer[start - i] << (8 * i);
    }
    return result;
};
