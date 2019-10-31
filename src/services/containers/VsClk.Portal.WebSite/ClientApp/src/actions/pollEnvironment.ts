import * as envRegService from '../services/envRegService';
import { useDispatch } from './middleware/useDispatch';
import { action } from './middleware/useActionCreator';
import { useActionContext } from './middleware/useActionContext';

import { stateChangeEnvironmentAction } from './environmentStateChange';

export const pollActivatingEnvironmentsActionType = 'async.environments.activating.poll';
export const pollActivatingEnvironmentsUpdateActionType =
    'async.environments.activating.poll.update';

// Basic actions dispatched for reducers
export const pollActivatingEnvironmentAction = (id: string) =>
    action(pollActivatingEnvironmentsActionType, { id });
export const pollActivatingEnvironmentUpdateAction = (id: string) =>
    action(pollActivatingEnvironmentsUpdateActionType, { id });

// Types to register with reducers
export type PollActivatingEnvironmentsAction = ReturnType<typeof pollActivatingEnvironmentAction>;
export type PollActivatingEnvironmentsUpdateAction = ReturnType<
    typeof pollActivatingEnvironmentUpdateAction
>;

export async function pollActivatingEnvironments() {
    const context = useActionContext();
    const {
        environments: { activatingEnvironments },
    } = context.state;

    await Promise.all(activatingEnvironments.map((id) => pollActivatingEnvironment(id)));
}

export async function pollActivatingEnvironment(id: string) {
    const dispatch = useDispatch();
    try {
        dispatch(pollActivatingEnvironmentAction(id));
        const environment = await envRegService.getEnvironment(id);

        if (!environment) {
            return;
        }

        dispatch(stateChangeEnvironmentAction(id, environment.state));
    } catch {
        dispatch(pollActivatingEnvironmentUpdateAction(id));
    }
}
