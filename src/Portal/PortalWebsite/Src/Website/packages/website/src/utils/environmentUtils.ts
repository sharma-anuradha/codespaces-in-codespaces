import {
    IEnvironment,
    EnvironmentStateInfo,
    isDefined,
    ILocalEnvironment,
    EnvironmentType,
    EnvironmentErrorCodes
} from 'vso-client-core';

import { ISku } from '../interfaces/ISku';
import { TFunction } from 'i18next';

export function isEnvironmentAvailable(
    local: ILocalEnvironment | undefined
): local is IEnvironment {
    return (
        isDefined(local) &&
        isDefined(local.id) &&
        isDefined(local.connection) &&
        isDefined(local.connection.sessionPath) &&
        local.state === EnvironmentStateInfo.Available
    );
}

export function isSelfHostedEnvironment({ type }: ILocalEnvironment) {
    return (
        isDefined(type) && type.toLowerCase() === EnvironmentType.StaticEnvironment.toLowerCase()
    );
}

export function environmentIsALie({ lieId }: ILocalEnvironment) {
    return lieId != null;
}

export function isNotAvailable({ state }: ILocalEnvironment): boolean {
    return state !== EnvironmentStateInfo.Available;
}

export function isNotSuspendable(environment: ILocalEnvironment): boolean {
    const { state } = environment;
    return (
        state === EnvironmentStateInfo.Provisioning ||
        state === EnvironmentStateInfo.Failed ||
        state === EnvironmentStateInfo.Shutdown ||
        state === EnvironmentStateInfo.Deleted ||
        isSelfHostedEnvironment(environment)
    );
}

export function isNotConnectable({ state }: ILocalEnvironment): boolean {
    return state !== EnvironmentStateInfo.Available &&
        state !== EnvironmentStateInfo.Shutdown &&
        state !== EnvironmentStateInfo.Provisioning &&
        state !== EnvironmentStateInfo.Starting;
}

export function isSuspended(cloudenvironment: ILocalEnvironment): boolean {
    return cloudenvironment.state === EnvironmentStateInfo.Shutdown;
}

export function isCreating(cloudenvironment: ILocalEnvironment): boolean {
    return cloudenvironment.state === EnvironmentStateInfo.Provisioning;
}

export function isStarting(cloudenvironment: ILocalEnvironment): boolean {
    return cloudenvironment.state === EnvironmentStateInfo.Starting ||
        cloudenvironment.state === EnvironmentStateInfo.Shutdown;
}

export function isActivating({ state }: Pick<ILocalEnvironment | IEnvironment, 'state'>) {
    switch (state) {
        case EnvironmentStateInfo.ShuttingDown:
        case EnvironmentStateInfo.Starting:
        case EnvironmentStateInfo.Provisioning:
        case EnvironmentStateInfo.Queued:

            return true;

        default:
            return false;
    }
}

export function isInStableState({ state }: ILocalEnvironment): boolean {
    return state === EnvironmentStateInfo.Available ||
        state === EnvironmentStateInfo.Shutdown;
}

export function stateToDisplayName(state: EnvironmentStateInfo, translation: TFunction) {
    switch (state) {
        case EnvironmentStateInfo.Queued:
            return translation('queued');
        case EnvironmentStateInfo.Provisioning:
            return translation('creating');
        case EnvironmentStateInfo.Failed:
            return translation('failedToCreate');
        case EnvironmentStateInfo.Shutdown:
            return translation('suspended');
        case EnvironmentStateInfo.ShuttingDown:
            return translation('suspending');
        default:
            return state;
    }
}

export function environmentErrorCodeToString(code: EnvironmentErrorCodes, translation: TFunction) {
    switch (code) {
        case EnvironmentErrorCodes.exceededQuota:
            return translation('exceededQuota');
        case EnvironmentErrorCodes.environmentNameAlreadyExists:
            return translation('environmentNameAlreadyExists');
        case EnvironmentErrorCodes.environmentDoesNotExist:
            return translation('environmentDoesNotExist');
        case EnvironmentErrorCodes.environmentNotAvailable:
            return translation('environmentNotAvailable');
        case EnvironmentErrorCodes.environmentNotShutdown:
            return translation('environmentNotShutdown');
        case EnvironmentErrorCodes.unableToAllocateResources:
            return translation('unableToAllocateResources');
        case EnvironmentErrorCodes.unableToAllocateResourcesWhileStarting:
            return translation('unableToAllocateResourcesWhileStarting');
        case EnvironmentErrorCodes.unableToUpdateSku:
            return translation('unableToUpdateSku');
        case EnvironmentErrorCodes.requestedSkuIsInvalid:
            return translation('requestedSkuIsInvalid');
        case EnvironmentErrorCodes.requestedAutoShutdownDelayMinutesIsInvalid:
            return translation('requestedAutoShutdownDelayMinutesIsInvalid');
        case  EnvironmentErrorCodes.heartbeatUnhealthy:
            return translation('heartbeatUnhealthy');
        case  EnvironmentErrorCodes.customContainersCreationFailed: 
            return translation('customContainersCreationFailed');
        case  EnvironmentErrorCodes.shutdownFailed: 
            return translation('errorCode') + ': 1001';
        case  EnvironmentErrorCodes.cMBMutexFailure: 
            return translation('errorCode') + ': 1002';
        case  EnvironmentErrorCodes.cMBGeneralError: 
            return translation('errorCode') + ': 1003';
        case  EnvironmentErrorCodes.startEnvironmentHandlerFailedToStartContainer: 
            return translation('startEnvironmentHandlerFailedToStartContainer');
        case  EnvironmentErrorCodes.startEnvironmentHandlerRequiredParameterMissing: 
            return translation('errorCode') + ': 1005';
        case  EnvironmentErrorCodes.startEnvironmentHandlerKitchensinkMissing: 
            return translation('errorCode') + ': 1006';
        case  EnvironmentErrorCodes.startEnvironmentHandlerLiveshareLoginFailed: 
            return translation('errorCode') + ': 1007';
        case  EnvironmentErrorCodes.startEnvironmentHandlerMoreThanOneContainerFoundOnRestart: 
            return translation('errorCode') + ': 1008';
        case  EnvironmentErrorCodes.customContainersGeneralError: 
            return translation('customContainersGeneralError');
        case  EnvironmentErrorCodes.customContainersKitchensinkCreationFailed: 
            return translation('customContainersKitchensinkCreationFailed');
        case  EnvironmentErrorCodes.customContainersKitchensinkStartFailed: 
            return translation('customContainersKitchensinkStartFailed');
        case  EnvironmentErrorCodes.customContainersCloneFailed: 
            return translation('customContainersCloneFailed');
        case  EnvironmentErrorCodes.customContainersPrivateClonetimeout : 
            return translation('customContainersPrivateClonetimeout');
        case  EnvironmentErrorCodes.customContainersCouldNotPullImage: 
            return translation('customContainersCouldNotPullImage');
        case  EnvironmentErrorCodes.customContainersCouldNotBuildUserImage : 
            return translation('customContainersCouldNotBuildUserImage');
        case  EnvironmentErrorCodes.customContainersCouldNotCreateUserContainer: 
            return translation('customContainersCouldNotCreateUserContainer');
        case  EnvironmentErrorCodes.customContainersCouldNotRunUserContainer: 
            return translation('customContainersCouldNotRunUserContainer');
        case  EnvironmentErrorCodes.customContainersCLICopyFailed :
            return translation('customContainersCLICopyFailed');
        case  EnvironmentErrorCodes.customContainersDependenciesFailed: 
            return translation('customContainersDependenciesFailed');
        case  EnvironmentErrorCodes.customContainersCLIStartFailed : 
            return translation('customContainersCLIStartFailed');
        case  EnvironmentErrorCodes.customContainersIncorrectUserID :
            return translation('customContainersIncorrectUserID');

        case  EnvironmentErrorCodes.customContainersComposeGeneralError :
            return translation('customContainersComposeGeneralError');
        case  EnvironmentErrorCodes.customContainersComposeValidationError :
            return translation('customContainersComposeValidationError');
        case  EnvironmentErrorCodes.customContainersComposeConfigError :
            return translation('customContainersComposeConfigError');
        case  EnvironmentErrorCodes.customContainersWrongServiceError :
            return translation('customContainersWrongServiceError');
        case  EnvironmentErrorCodes.customContainersComposeUpError :
            return translation('customContainersComposeUpError');

        case EnvironmentErrorCodes.shutdownStaticEnvironment:
        case EnvironmentErrorCodes.startStaticEnvironment:
        case EnvironmentErrorCodes.unknown:
        default:
            return EnvironmentErrorCodes[code];
    }
}

const STANDARD_SPECS = '4 cores, 8 GB RAM';
const PREMIUM_SPECS = '8 cores, 16 GB RAM';
export function getSkuSpecLabel(sku: ISku, translation: TFunction) {
    switch (sku.name) {
        case 'standardLinux':
            return `${translation('standardLinux')}: ${STANDARD_SPECS}`;
        case 'premiumLinux':
            return `${translation('premiumLinux')}: ${PREMIUM_SPECS}`;
        case 'premiumWindows':
            return `${translation('premiumWindows')}: ${PREMIUM_SPECS}`;
        default:
            return sku.displayName;
    }
}
