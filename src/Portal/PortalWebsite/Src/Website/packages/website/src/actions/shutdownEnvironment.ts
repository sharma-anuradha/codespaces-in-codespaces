import { EnvironmentStateInfo, IEnvironment } from 'vso-client-core';

import * as envRegService from '../services/envRegService';
import { useDispatch } from './middleware/useDispatch';
import { stateChangeEnvironmentAction } from './environmentStateChange';

// Exposed - callable actions that have side-effects
export async function shutdownEnvironment(environemntInfo: IEnvironment) {
    const dispatch = useDispatch();
    const { id, state } = environemntInfo;

    // 1. Try to shutdown environment
    try {
        dispatch(stateChangeEnvironmentAction(id, EnvironmentStateInfo.ShuttingDown, state));
        await envRegService.shutdownEnvironment(environemntInfo);
    } catch (err) {
        let e = await envRegService.getEnvironment(id);
        if (e) {
            dispatch(stateChangeEnvironmentAction(id, e!.state, state));
        }

        throw err;
    }
}
