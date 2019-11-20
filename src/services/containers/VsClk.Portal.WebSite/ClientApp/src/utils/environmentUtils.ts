import { isDefined } from './isDefined';
import {
    ILocalCloudEnvironment,
    ICloudEnvironment,
    StateInfo,
    EnvironmentType,
    EnvironmentErrorCodes,
} from '../interfaces/cloudenvironment';
import { ISku } from '../interfaces/ISku';

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
    return state !== StateInfo.Available &&
        state !== StateInfo.Shutdown &&
        state !== StateInfo.Provisioning &&
        state !== StateInfo.Starting;
}

export function isSuspended(cloudenvironment: ILocalCloudEnvironment): boolean {
    return cloudenvironment.state === StateInfo.Shutdown;
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

export function stateToDisplayName(state: StateInfo) {
    switch (state) {
        case StateInfo.Provisioning:
            return 'Creating';
        case StateInfo.Failed:
            return 'Failed to Create';
        case StateInfo.Shutdown:
            return 'Suspended';
        case StateInfo.ShuttingDown:
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
