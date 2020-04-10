import {
    IEnvironment,
    EnvironmentStateInfo,
    isDefined,
    ILocalEnvironment,
    EnvironmentType,
    EnvironmentErrorCodes
} from 'vso-client-core';

import { ISku } from '../interfaces/ISku';

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

export function isActivating({ state }: Pick<ILocalEnvironment | IEnvironment, 'state'>) {
    switch (state) {
        case EnvironmentStateInfo.ShuttingDown:
        case EnvironmentStateInfo.Starting:
        case EnvironmentStateInfo.Provisioning:
            return true;

        default:
            return false;
    }
}

export function isInStableState({ state }: ILocalEnvironment): boolean {
    return state === EnvironmentStateInfo.Available ||
        state === EnvironmentStateInfo.Shutdown;
}

export function stateToDisplayName(state: EnvironmentStateInfo) {
    switch (state) {
        case EnvironmentStateInfo.Provisioning:
            return 'Creating';
        case EnvironmentStateInfo.Failed:
            return 'Failed to Create';
        case EnvironmentStateInfo.Shutdown:
            return 'Suspended';
        case EnvironmentStateInfo.ShuttingDown:
            return 'Suspending';
        default:
            return state;
    }
}

export function environmentErrorCodeToString(code: EnvironmentErrorCodes) {
    switch (code) {
        case EnvironmentErrorCodes.exceededQuota:
            return 'You have exceeded the environment quota.';
        case EnvironmentErrorCodes.environmentNameAlreadyExists:
            return 'Cloud environment already exists.';
        case EnvironmentErrorCodes.environmentDoesNotExist:
            return 'Cloud environment does not exist.';
        case EnvironmentErrorCodes.environmentNotAvailable:
            return 'Environment is not in available state.';
        case EnvironmentErrorCodes.environmentNotShutdown:
            return 'Environment is not in suspended state.';
        case EnvironmentErrorCodes.unableToAllocateResources:
            return 'Please try again in a few minutes or select a plan in another location.';
        case EnvironmentErrorCodes.unableToAllocateResourcesWhileStarting:
            return 'Unable to start the environment. Please try again in a few minutes.';
        case EnvironmentErrorCodes.unableToUpdateSku:
            return 'Environment\'s current instance type does not support any changes.';
        case EnvironmentErrorCodes.requestedSkuIsInvalid:
            return 'Environment\'s current instance type does not support the requested instance type.';
        case EnvironmentErrorCodes.requestedAutoShutdownDelayMinutesIsInvalid:
            return 'Requested auto-suspend delay is invalid.';
        case  EnvironmentErrorCodes.heartbeatUnhealthy:
            return 'The environment was reported as unhealthy, suspend and restart the environment.';
        case  EnvironmentErrorCodes.customContainersCreationFailed: 
            return 'The environment creation based on devcontainer.json failed, please review the console for more details.';
        case  EnvironmentErrorCodes.shutdownFailed: 
            return 'Error Code: 1001';
        case  EnvironmentErrorCodes.cMBMutexFailure: 
            return 'Error Code: 1002';
        case  EnvironmentErrorCodes.cMBGeneralError: 
            return 'Error Code: 1003';
        case  EnvironmentErrorCodes.startEnvironmentHandlerFailedToStartContainer: 
            return 'Failed to start container.';
        case  EnvironmentErrorCodes.startEnvironmentHandlerRequiredParameterMissing: 
            return 'Error Code: 1005';
        case  EnvironmentErrorCodes.startEnvironmentHandlerKitchensinkMissing: 
            return 'Error Code: 1006';
        case  EnvironmentErrorCodes.startEnvironmentHandlerLiveshareLoginFailed: 
            return 'Error Code: 1007';
        case  EnvironmentErrorCodes.startEnvironmentHandlerMoreThanOneContainerFoundOnRestart: 
            return 'Error Code: 1008';
        case  EnvironmentErrorCodes.customContainersGeneralError: 
            return 'Unknown error in environment creation.';
        case  EnvironmentErrorCodes.customContainersKitchensinkCreationFailed: 
            return 'Failed to create container with standard image.';
        case  EnvironmentErrorCodes.customContainersKitchensinkStartFailed: 
            return 'Failed to start standard container.';
        case  EnvironmentErrorCodes.customContainersCloneFailed: 
            return 'The repository could not be cloned.';
        case  EnvironmentErrorCodes.customContainersPrivateClonetimeout : 
            return 'Timeout waiting while attempting to clone a private repository.';
        case  EnvironmentErrorCodes.customContainersCouldNotPullImage: 
            return 'Could not pull the image referenced in the DockerFile.';
        case  EnvironmentErrorCodes.customContainersCouldNotBuildUserImage : 
            return 'The custom container could not be built.';
        case  EnvironmentErrorCodes.customContainersCouldNotCreateUserContainer: 
            return 'The custom container failed to create.';
        case  EnvironmentErrorCodes.customContainersCouldNotRunUserContainer: 
            return 'The custom container failed to run.';
        case  EnvironmentErrorCodes.customContainersCLICopyFailed :
            return  'Failed to copy the VSOnline Agent to the custom container.';
        case  EnvironmentErrorCodes.customContainersDependenciesFailed: 
            return 'The VSOnline Agent dependencies failed to install in the custom container.';
        case  EnvironmentErrorCodes.customContainersCLIStartFailed : 
            return 'The VSOnline Agent failed to start in the custom container.';

        case EnvironmentErrorCodes.shutdownStaticEnvironment:
        case EnvironmentErrorCodes.startStaticEnvironment:
        case EnvironmentErrorCodes.unknown:
        default:
            return EnvironmentErrorCodes[code];
    }
}

const STANDARD_SPECS = '4 cores, 8 GB RAM';
const PREMIUM_SPECS = '8 cores, 16 GB RAM';
export function getSkuSpecLabel(sku: ISku) {
    switch (sku.name) {
        case 'standardLinux':
            return `Standard (Linux): ${STANDARD_SPECS}`;
        case 'premiumLinux':
            return `Premium (Linux): ${PREMIUM_SPECS}`;
        case 'premiumWindows':
            return `Premium (Windows): ${PREMIUM_SPECS}`;
        default:
            return sku.displayName;
    }
}
