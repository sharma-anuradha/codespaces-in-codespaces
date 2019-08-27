import * as envRegService from '../services/envRegService';
import { wait } from '../dependencies';
import { ICloudEnvironment, StateInfo } from '../interfaces/cloudenvironment';
import { useDispatch } from './middleware/useDispatch';
import { action } from './middleware/useActionCreator';

export const pollEnvironmentActionType = 'async.environments.poll';
export const pollEnvironmentWaitingActionType = 'async.environments.poll.waiting';
export const pollEnvironmentUpdateActionType = 'async.environments.poll.update';
export const pollEnvironmentSuccessActionType = 'async.environments.poll.success';
export const pollEnvironmentFailureActionType = 'async.environments.poll.failure';

// Basic actions dispatched for reducers
const pollEnvironmentAction = (id: string) => action(pollEnvironmentActionType, { id });
const pollEnvironmentWaitingAction = (id: string) =>
    action(pollEnvironmentWaitingActionType, { id });
const pollEnvironmentUpdateAction = (environment: ICloudEnvironment) =>
    action(pollEnvironmentUpdateActionType, { environment });
const pollEnvironmentSuccessAction = (environment: ICloudEnvironment) =>
    action(pollEnvironmentSuccessActionType, { environment });
const pollEnvironmentFailureAction = (id: string, error: Error) =>
    action(pollEnvironmentFailureActionType, { id }, error);

// Types to register with reducers
export type PollEnvironmentAction = ReturnType<typeof pollEnvironmentAction>;
export type PollEnvironmentWaitingAction = ReturnType<typeof pollEnvironmentWaitingAction>;
export type PollEnvironmentUpdateAction = ReturnType<typeof pollEnvironmentUpdateAction>;
export type PollEnvironmentSuccessAction = ReturnType<typeof pollEnvironmentSuccessAction>;
export type PollEnvironmentFailureAction = ReturnType<typeof pollEnvironmentFailureAction>;

// Exposed - callable actions that have side-effects
export async function pollEnvironment(id: string) {
    const dispatch = useDispatch();
    try {
        dispatch(pollEnvironmentAction(id));

        // Stop polling after 2 minutes. If not done, user will probably refresh anyway.
        const limit = Date.now() + 2 * 60 * 1000;
        let environment: ICloudEnvironment | undefined;
        while (Date.now() < limit) {
            environment = await isEnvironmentValidYet();

            if (environment) {
                break;
            }
        }

        if (environment) {
            dispatch(pollEnvironmentSuccessAction(environment));
        } else {
            dispatch(
                pollEnvironmentFailureAction(
                    id,
                    new Error('Failed to get connection info for environment in over 2 minutes.')
                )
            );
        }
    } catch (err) {
        dispatch(pollEnvironmentFailureAction(id, err));
    }

    async function isEnvironmentValidYet() {
        let environment = await envRegService.getEnvironment(id);
        if (!environment) {
            dispatch(pollEnvironmentWaitingAction(id));
            await wait(1000);

            return;
        }

        const {
            connection: { sessionId, sessionPath } = {
                sessionId: undefined,
                sessionPath: undefined,
            },
        } = environment;

        if (!sessionId || sessionPath) {
            await wait(1000);
            dispatch(pollEnvironmentWaitingAction(id));

            return;
        }

        if (environment.state.toString() === StateInfo.Available) {
            return environment;
        }

        await wait(1000);
        dispatch(pollEnvironmentUpdateAction(environment));
    }
}
