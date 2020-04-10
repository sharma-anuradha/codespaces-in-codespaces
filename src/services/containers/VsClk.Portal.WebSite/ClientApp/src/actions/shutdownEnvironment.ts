import { EnvironmentStateInfo } from 'vso-client-core';

import * as envRegService from '../services/envRegService';
import { useDispatch } from './middleware/useDispatch';
import { stateChangeEnvironmentAction } from './environmentStateChange';

// Exposed - callable actions that have side-effects
export async function shutdownEnvironment(id: string, oldState: EnvironmentStateInfo) {
    const dispatch = useDispatch();
    // 1. Try to shutdown environment
    try {
        dispatch(stateChangeEnvironmentAction(id, EnvironmentStateInfo.ShuttingDown, oldState));
        await envRegService.shutdownEnvironment(id);
    } catch (err) {
        let e = await envRegService.getEnvironment(id);
        if (e) {
            dispatch(stateChangeEnvironmentAction(id, e!.state, oldState));
        }

        throw err;
    }
}
