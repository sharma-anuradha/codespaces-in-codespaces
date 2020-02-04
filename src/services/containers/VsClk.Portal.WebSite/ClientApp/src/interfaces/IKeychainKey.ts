import { SupportedCipherAlgorithms, SupportedCipherModes } from '../cache/localStorageKeychain/webCipher';

export interface IKeychainKey extends IKeychainKeyBase {
    readonly method: SupportedCipherAlgorithms;
    readonly methodMode: SupportedCipherModes;
    readonly key: Buffer;
}

export interface IKeychainKeyWithoutMethods extends IKeychainKeyBase {
    readonly key: string;
}

interface IKeychainKeyBase {
    readonly id: string;
    readonly expiresOn: number;
}
