import { isDefined } from './isDefined';
import {
    ILocalCloudEnvironment,
    ICloudEnvironment,
    StateInfo,
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

export function environmentIsALie({ lieId }: ILocalCloudEnvironment) {
    return lieId != null;
}

export function isNotSuspendable({ state }: ILocalCloudEnvironment): boolean {
    return state === StateInfo.Provisioning ||
            state === StateInfo.Failed ||
            state === StateInfo.Shutdown ||
            state === StateInfo.Deleted
}

export function isNotConnectable({ state }: ILocalCloudEnvironment): boolean {
    return state !== StateInfo.Available && state !== StateInfo.Shutdown;
}