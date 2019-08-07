import { ICloudEnvironment } from '../interfaces/cloudenvironment';
import envRegService from '../services/envRegService';

import { action, Dispatch } from './actionUtils';

export const fetchEnvironmentsActionType = 'async.environments.fetch';
export const fetchEnvironmentsSuccessActionType = 'async.environments.fetch.success';
export const fetchEnvironmentsFailureActionType = 'async.environments.fetch.failure';

// Basic actions dispatched for reducers
const fetchEnvironmentsAction = () => action(fetchEnvironmentsActionType);
const fetchEnvironmentsSuccessAction = (environments: ICloudEnvironment[]) =>
    action(fetchEnvironmentsSuccessActionType, { environments });
const fetchEnvironmentsFailureAction = (error: Error) =>
    action(fetchEnvironmentsFailureActionType, undefined, error);

// Types to register with reducers
export type FetchEnvironmentsAction = ReturnType<typeof fetchEnvironmentsAction>;
export type FetchEnvironmentsSuccessAction = ReturnType<typeof fetchEnvironmentsSuccessAction>;
export type FetchEnvironmentsFailureAction = ReturnType<typeof fetchEnvironmentsFailureAction>;

// Exposed - callable actions that have side-effects
export const fetchEnvironments = () => async (dispatch: Dispatch) => {
    try {
        dispatch(fetchEnvironmentsAction());
        const environments = await envRegService.fetchEnvironments();
        dispatch(fetchEnvironmentsSuccessAction(environments));
    } catch (err) {
        dispatch(fetchEnvironmentsFailureAction(err));
    }
};
