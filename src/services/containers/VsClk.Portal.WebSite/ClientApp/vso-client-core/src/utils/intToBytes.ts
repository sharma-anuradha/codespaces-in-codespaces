export const intToBytes = (num: number, bufferLength = 4): Buffer => {
    const buff = new Buffer(bufferLength);
    const start = (bufferLength - 1);
    for (let i = start; i >= 0; i--) {
        buff[i] = (num >> (8 * i)) & 255;
    }
    
    return buff.reverse();
};
