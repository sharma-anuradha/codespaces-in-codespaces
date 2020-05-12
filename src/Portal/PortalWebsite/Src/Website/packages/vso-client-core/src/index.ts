// auth
export { PostMessageChannel } from './postMessageChannel/postMessageChannel';
export { authService } from './auth/authService';

// keychain
export { addDefaultGithubKey } from './keychain/localstorageKeychainKeys';
export { setKeychainKeys, addRandomKey, getRandomKey } from './keychain/localstorageKeychainKeys';

// utils
export { Signal, CancellationError } from './utils/Signal';
export { randomString } from './utils/randomString';
export { randomBytes } from './utils/randomBytes';
export { isThenable } from './utils/isThenable';
export { isGithubTLD } from './utils/isGithubTLD';
export { isHostedOnGithub } from './utils/isHostedOnGithub';
export { getFeatureSet } from './utils/getFeatureSet';
export { getVSCodeScheme } from './utils/getVSCodeScheme';
export { getCurrentEnvironmentId } from './utils/getCurrentEnvironmentId';
export { isInIframe } from './utils/isInIframe';
export { wait } from './utils/wait';
export { isDefined } from './utils/isDefined';
export { bufferToArrayBuffer } from './utils/bufferToArrayBuffer';
export { createCancellationToken } from './utils/timer';
export { debounceInterval } from './utils/debounceInterval';

// interfaces
export { IKeychain } from './interfaces/IKeychain';
export { IKeychainKey } from './interfaces/IKeychainKey';
export { IKeychainKeyWithoutMethods } from './interfaces/IKeychainKey';
export { ITelemetryEvent, TelemetryPropertyValue } from './interfaces/ITelemetryEvent';
export { localStorageKeychain } from './keychain/localstorageKeychain';
export { createKeys } from './keychain/createKeys';
export { enhanceEncryptionKeys } from './keychain/enhanceEncryptionKeys';
export { IEnvironment, EnvironmentStateInfo, ILocalEnvironment } from './interfaces/IEnvironment';
export { ILiveShareClient, IWorkspaceInfo, IWorkspaceAccess } from './interfaces/ILiveShareClient';

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
export { IPartnerInfo } from './interfaces/IPartnerInfo';
export { EnvironmentType, EnvironmentErrorCodes } from './interfaces/IEnvironment';
export { FeatureSet } from './interfaces/FeatureSet';

// logging & telemetry
export { createTrace, maybePii, Trace } from './utils/createTrace';
export { TelemetryService } from './telemetry/TelemetryService';
export { cleanupPII, cleanupPIIForExternal } from './telemetry/cleanupPII';

// constants
import { SECOND_MS, MINUTE_MS, HOUR_MS, DAY_MS } from './constants';
export const timeConstants = { SECOND_MS, MINUTE_MS, HOUR_MS, DAY_MS };
export { KNOWN_VSO_HOSTNAMES } from './constants';

export { RequestStore } from './utils/RequestStore';
