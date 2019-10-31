import { isDefined } from './isDefined';
import {
    ILocalCloudEnvironment,
    ICloudEnvironment,
    StateInfo,
    EnvironmentType,
    EnvironmentErrorCodes,
} from '../interfaces/cloudenvironment';

export function isEnvironmentAvailable(
    local: ILocalCloudEnvironment | undefined
): local is ICloudEnvironment {
    return (
        isDefined(local) &&
        isDefined(local.id) &&
        isDefined(local.connection) &&
        local.state === StateInfo.Available
    );
}

export function isSelfHostedEnvironment({ type }: ILocalCloudEnvironment) {
    return (
        isDefined(type) && type.toLowerCase() === EnvironmentType.StaticEnvironment.toLowerCase()
    );
}

export function environmentIsALie({ lieId }: ILocalCloudEnvironment) {
    return lieId != null;
}

export function isNotAvailable({ state }: ILocalCloudEnvironment): boolean {
    return state !== StateInfo.Available;
}

export function isNotSuspendable(environment: ILocalCloudEnvironment): boolean {
    const { state } = environment;
    return (
        state === StateInfo.Provisioning ||
        state === StateInfo.Failed ||
        state === StateInfo.Shutdown ||
        state === StateInfo.Deleted ||
        isSelfHostedEnvironment(environment)
    );
}

export function isNotConnectable({ state }: ILocalCloudEnvironment): boolean {
    return state !== StateInfo.Available && state !== StateInfo.Shutdown;
}

export function isActivating({ state }: Pick<ILocalCloudEnvironment | ICloudEnvironment, 'state'>) {
    switch (state) {
        case StateInfo.ShuttingDown:
        case StateInfo.Starting:
        case StateInfo.Provisioning:
            return true;

        default:
            return false;
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

        case EnvironmentErrorCodes.shutdownStaticEnvironment:
        case EnvironmentErrorCodes.startStaticEnvironment:
        case EnvironmentErrorCodes.unknown:
        default:
            return EnvironmentErrorCodes[code];
    }
}
