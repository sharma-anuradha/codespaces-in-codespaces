export interface ICipher {
    readonly blockLength: number;
    transform(data: Buffer): Promise<Buffer>;
}
