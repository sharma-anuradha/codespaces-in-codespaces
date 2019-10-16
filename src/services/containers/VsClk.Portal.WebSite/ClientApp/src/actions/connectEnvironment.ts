import * as envRegService from '../services/envRegService';
import { useDispatch } from './middleware/useDispatch';
import { StateInfo, ICloudEnvironment } from '../interfaces/cloudenvironment';
import { stateChangeEnvironmentAction } from './environmentStateChange'

// Exposed - callable actions that have side-effects
export async function connectEnvironment(id: string, environmentState: StateInfo): Promise<ICloudEnvironment | undefined> {
    // 1. Try to connect environment
    const dispatch = useDispatch();
    try {
        if (environmentState === StateInfo.Shutdown) {
            dispatch(stateChangeEnvironmentAction(id, StateInfo.Starting));
        }

        return await envRegService.connectEnvironment(id, environmentState);
    } catch (err) {
        // Noop
        if (environmentState === StateInfo.Shutdown) {
            // If starting environment failed, put it to right state.
            let e = await envRegService.getEnvironment(id);
            dispatch(stateChangeEnvironmentAction(id, e!.state));
        }

        throw err;
    }
}
