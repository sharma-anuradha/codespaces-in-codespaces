import envRegService from '../services/envRegService';
import { action, Dispatch } from './actionUtils';

export const deleteEnvironmentActionType = 'async.environments.delete';
export const deleteEnvironmentSuccessActionType = 'async.environments.delete.success';
export const deleteEnvironmentFailureActionType = 'async.environments.delete.failure';

// Basic actions dispatched for reducers
const deleteEnvironmentAction = (id: string) => action(deleteEnvironmentActionType, { id });
const deleteEnvironmentSuccessAction = (id: string) =>
    action(deleteEnvironmentSuccessActionType, { id });
const deleteEnvironmentFailureAction = (id: string, error: Error) =>
    action(deleteEnvironmentFailureActionType, { id }, error);

// Types to register with reducers
export type DeleteEnvironmentAction = ReturnType<typeof deleteEnvironmentAction>;
export type DeleteEnvironmentSuccessAction = ReturnType<typeof deleteEnvironmentSuccessAction>;
export type DeleteEnvironmentFailureAction = ReturnType<typeof deleteEnvironmentFailureAction>;

// Exposed - callable actions that have side-effects
export const deleteEnvironment = (id: string) => async (dispatch: Dispatch) => {
    // 1. Try to delete environment
    try {
        dispatch(deleteEnvironmentAction(id));
        await envRegService.deleteEnvironment(id);
        dispatch(deleteEnvironmentSuccessAction(id));
    } catch (err) {
        dispatch(deleteEnvironmentFailureAction(id, err));
    }
};
