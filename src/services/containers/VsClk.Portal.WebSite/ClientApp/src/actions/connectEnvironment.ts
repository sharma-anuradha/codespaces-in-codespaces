import * as envRegService from '../services/envRegService';
import { useDispatch } from './middleware/useDispatch';
import { StateInfo, ICloudEnvironment } from '../interfaces/cloudenvironment';
import { stateChangeEnvironmentAction } from './environmentStateChange';
import { ServiceResponseError } from './middleware/useWebClient';
import { environmentErrorCodeToString } from '../utils/environmentUtils';

// Exposed - callable actions that have side-effects
export async function connectEnvironment(
    id: string,
    environmentState: StateInfo
): Promise<ICloudEnvironment | undefined> {
    // 1. Try to connect environment
    const dispatch = useDispatch();
    try {
        if (environmentState === StateInfo.Shutdown) {
            dispatch(stateChangeEnvironmentAction(id, StateInfo.Starting, true));
        }

        return await envRegService.connectEnvironment(id, environmentState);
    } catch (err) {
        if (err instanceof ServiceResponseError) {
            await updateErrorMessage();

            async function updateErrorMessage() {
                let text = undefined;
                try {
                    text = await err.response.text();
                } catch {
                    return;
                }

                // We have two types of error responses
                // - code
                // - actual error message
                // We'll normalize them after ignite.
                try {
                    const errorCode = JSON.parse(text);
                    if (typeof errorCode !== 'number') {
                        throw new Error();
                    }
                    err.message = environmentErrorCodeToString(errorCode);
                } catch {
                    err.message = text;
                }
            }
        }

        // Noop
        if (environmentState === StateInfo.Shutdown) {
            // If starting environment failed, put it to right state.
            let e = await envRegService.getEnvironment(id);
            dispatch(stateChangeEnvironmentAction(id, e!.state, true));
        }

        throw err;
    }
}
