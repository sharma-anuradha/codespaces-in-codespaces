import * as envRegService from '../services/envRegService';
import { useDispatch } from './middleware/useDispatch';
import { action } from './middleware/useActionCreator';
import { useActionContext } from './middleware/useActionContext';
import { environmentChangedAction } from './environmentChanged';
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

    if (activatingEnvironments.length === 0) {
        return;
    }

    await Promise.all(activatingEnvironments.map((id) => pollActivatingEnvironment(id)));
}

export async function pollActivatingEnvironment(id: string) {
    const dispatch = useDispatch();
    const activatingEnvironments = useActionContext().state.environments.environments;
    try {
        const context = useActionContext();
        const envWithOldState = activatingEnvironments.find((item) => item.id === id);
        const oldState = envWithOldState && envWithOldState.state;

        dispatch(pollActivatingEnvironmentAction(id));

        const environment = await envRegService.getEnvironment(id);

        if (!environment) {
            return;
        }
        if (oldState !== environment.state) {
            dispatch(stateChangeEnvironmentAction(id, environment.state, oldState, context));
        }
        dispatch(environmentChangedAction(environment));
    } catch {
        dispatch(pollActivatingEnvironmentUpdateAction(id));
    }
}
