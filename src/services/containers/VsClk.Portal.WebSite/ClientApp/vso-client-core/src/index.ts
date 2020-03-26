// auth
export { PostMessageChannel } from './postMessageChannel/postMessageChannel';
export { authService } from './auth/authService';

// keychain
export { addDefaultGithubKey } from './keychain/localstorageKeychainKeys';
export { setKeychainKeys, addRandomKey } from './keychain/localstorageKeychainKeys';

// utils
export { Signal, CancellationError } from './utils/Signal';
export { randomString } from './utils/randomString';
export { randomBytes } from './utils/randomBytes';
export { isThenable } from './utils/isThenable';
export { isGithubTLD } from './utils/isGithubTLD';
export { isInIframe } from './utils/isInIframe';

// interfaces
export { IKeychain } from './interfaces/IKeychain';
export { IKeychainKey } from './interfaces/IKeychainKey';
export { IKeychainKeyWithoutMethods } from './interfaces/IKeychainKey';
export { ITelemetryEvent, TelemetryPropertyValue } from './interfaces/ITelemetryEvent';
export { localStorageKeychain } from './keychain/localstorageKeychain';
export { createKeys } from './keychain/createKeys';
export { enhanceEncryptionKeys } from './keychain/enhanceEncryptionKeys';

// logging & telemetry
export { createTrace, maybePii } from './utils/createTrace';
export { TelemetryService } from './telemetry/TelemetryService';
export { cleanupPII, cleanupPIIForExternal } from './telemetry/cleanupPII';
