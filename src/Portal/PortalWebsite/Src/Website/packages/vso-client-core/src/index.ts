// auth
export { PostMessageChannel } from './postMessageChannel/postMessageChannel';
export { authService } from './auth/authService';

// keychain
export {
    setKeychainKeys,
    addRandomKey,
    addDefaultGithubKey,
    getRandomKey,
} from './keychain/localstorageKeychainKeys';
export { fetchKeychainKeys } from './keychain/fetchKeychainKeys';
export { createKeys } from './keychain/createKeys';

// utils
export { Signal, CancellationError } from './utils/Signal';
export { randomString } from './utils/randomString';
export { randomBytes } from './utils/randomBytes';
export { isThenable } from './utils/isThenable';
export { isGithubTLD, isGithubDotDevTLD, isGithubLocalTLD } from './utils/isGithubTLD';
export { isSalesforceTLD } from './utils/isSalesforceTLD';
export { isKnownPartnerTLD } from './utils/isKnownPartnerTLD';
export { isHostedOnGithub } from './utils/isHostedOnGithub';
export { getFeatureSet, setFeatureSet } from './utils/getFeatureSet';
export { getVSCodeScheme } from './utils/getVSCodeScheme';
export {
    getCurrentEnvironmentId,
    setCurrentCodespaceId,
    tryGetCurrentEnvironmentId,
} from './utils/getCurrentEnvironmentId';
export { isInIframe } from './utils/isInIframe';
export { wait } from './utils/wait';
export { isDefined } from './utils/isDefined';
export { bufferToArrayBuffer } from './utils/bufferToArrayBuffer';
export { createCancellationToken } from './utils/timer';
export { debounceInterval } from './utils/debounceInterval';
export { RequestStore } from './utils/RequestStore';
export { arrayUnique } from './utils/arrayUnique';
export { updateFavicon } from './utils/updateFavicon';
export { getParentDomain } from './utils/getParentDomain';
export { cookies } from './utils/cookies';

// interfaces
export { IKeychain } from './interfaces/IKeychain';
export { IKeychainKey } from './interfaces/IKeychainKey';
export { IKeychainKeyWithoutMethods } from './interfaces/IKeychainKey';
export { ITelemetryEvent, TelemetryPropertyValue } from './interfaces/ITelemetryEvent';
export { localStorageKeychain } from './keychain/localstorageKeychain';
export { enhanceEncryptionKeys } from './keychain/enhanceEncryptionKeys';
export { IEnvironment, EnvironmentStateInfo, ILocalEnvironment } from './interfaces/IEnvironment';
export { ILiveShareClient, IWorkspaceInfo, IWorkspaceAccess } from './interfaces/ILiveShareClient';
export {
    ISecret,
    ICreateSecretRequest,
    IUpdateSecretRequest,
    SecretScope,
    SecretType,
    FilterType,
    ISecretFilter,
    SecretAction,
    SecretErrorCodes,
} from './interfaces/ISecret';
export { TCodespaceEnvironment } from './interfaces/TCodespaceEnvironment';

import * as vsls from './interfaces/vsls';
export { vsls };

import * as vslsTypes from './interfaces/vsls-types';
export { vslsTypes };

export {
    EnvironmentConfigurationService,
    environmentConfigurationService,
} from './interfaces/services';
export {
    VSCodeServerOptions,
    vsCodeServerHostService,
    VSCodeServerHostService,
} from './interfaces/services';
export { TVSCodeQuality } from './interfaces/TVSCodeQuality';
export { IVSCodeConfig } from './interfaces/IVSCodeConfig';
export { ICredentialsProvider } from './interfaces/ICredentialsProvider';
export { IGitCredential } from './interfaces/IGitCredential';
export { TPostMessageChannelMessages } from './interfaces/TPostMessageChannelMessages';
export { IPartnerInfo, TSupportedNativeVSCodeAuthProviders } from './interfaces/IPartnerInfo';
export { EnvironmentType, EnvironmentErrorCodes } from './interfaces/IEnvironment';
export { FeatureSet } from './interfaces/FeatureSet';
export { TCodespaceInfo } from './interfaces/TCodespaceInfo';

// logging & telemetry
export { createTrace, maybePii, Trace } from './utils/createTrace';
export { TelemetryService } from './telemetry/TelemetryService';
export { cleanupPII, cleanupPIIForExternal } from './telemetry/cleanupPII';

// constants
export * from './constants';
export { timeConstants } from './timeConstants';
