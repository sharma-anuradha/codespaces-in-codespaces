import { SupportedCipherAlgorithms, SupportedCipherModes } from '../cache/localStorageKeyVault/webCipher';

export interface IKeyVaultKey {
    readonly id: string;
    readonly key: Buffer;
    readonly expiresOn: number;
    readonly method: SupportedCipherAlgorithms;
    readonly methodMode: SupportedCipherModes;
}
