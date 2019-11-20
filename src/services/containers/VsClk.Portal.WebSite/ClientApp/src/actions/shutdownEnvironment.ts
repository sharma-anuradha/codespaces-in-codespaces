﻿import * as envRegService from '../services/envRegService';
import { useDispatch } from './middleware/useDispatch';
import { StateInfo } from '../interfaces/cloudenvironment';
import { stateChangeEnvironmentAction } from './environmentStateChange';

// Exposed - callable actions that have side-effects
export async function shutdownEnvironment(id: string) {
    const dispatch = useDispatch();
    // 1. Try to shutdown environment
    try {
        dispatch(stateChangeEnvironmentAction(id, StateInfo.ShuttingDown));
        await envRegService.shutdownEnvironment(id);
    } catch (err) {
        let e = await envRegService.getEnvironment(id);
        if (e) {
            dispatch(stateChangeEnvironmentAction(id, e!.state));
        }

        throw err;
    }
}
