import { isDefined } from './isDefined';
import {
    ILocalCloudEnvironment,
    ICloudEnvironment,
    StateInfo,
    EnvironmentType,
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
    return isDefined(type) && type.toLowerCase() === EnvironmentType.StaticEnvironment.toLowerCase();
}

export function environmentIsALie({ lieId }: ILocalCloudEnvironment) {
    return lieId != null;
}

export function isNotAvailable({ state }: ILocalCloudEnvironment): boolean {
    return state !== StateInfo.Available;
}

export function isNotSuspendable(environment: ILocalCloudEnvironment): boolean {
    const { state } = environment;
    return state === StateInfo.Provisioning ||
            state === StateInfo.Failed ||
            state === StateInfo.Shutdown ||
            state === StateInfo.Deleted ||
            isSelfHostedEnvironment(environment);
}

export function isNotConnectable({ state }: ILocalCloudEnvironment): boolean {
    return state !== StateInfo.Available && state !== StateInfo.Shutdown;
}
