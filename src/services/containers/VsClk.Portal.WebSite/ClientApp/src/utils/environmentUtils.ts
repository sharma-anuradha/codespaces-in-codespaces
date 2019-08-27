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

export function isNotAvailable({ state }: ILocalCloudEnvironment): boolean {
    return state !== StateInfo.Available;
}
