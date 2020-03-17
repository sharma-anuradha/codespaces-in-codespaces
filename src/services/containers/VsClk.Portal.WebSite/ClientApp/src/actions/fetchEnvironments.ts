import { ICloudEnvironment } from '../interfaces/cloudenvironment';
import * as envRegService from '../services/envRegService';

import { action } from './middleware/useActionCreator';
import { useDispatch } from './middleware/useDispatch';
import { ServiceAuthenticationError } from './middleware/useWebClient';
import { logout } from './logout';

export const fetchEnvironmentsActionType = 'async.environments.fetch';
export const fetchEnvironmentsSuccessActionType = 'async.environments.fetch.success';
export const fetchEnvironmentsFailureActionType = 'async.environments.fetch.failure';

// Basic actions dispatched for reducers
const fetchEnvironmentsAction = () => action(fetchEnvironmentsActionType);
const fetchEnvironmentsSuccessAction = (environments: ICloudEnvironment[]) =>
    action(fetchEnvironmentsSuccessActionType, { environments });
const fetchEnvironmentsFailureAction = (error: Error) =>
    action(fetchEnvironmentsFailureActionType, error);

// Types to register with reducers
export type FetchEnvironmentsAction = ReturnType<typeof fetchEnvironmentsAction>;
export type FetchEnvironmentsSuccessAction = ReturnType<typeof fetchEnvironmentsSuccessAction>;
export type FetchEnvironmentsFailureAction = ReturnType<typeof fetchEnvironmentsFailureAction>;

// Exposed - callable actions that have side-effects
export async function fetchEnvironments() {
    const dispatch = useDispatch();
    try {
        dispatch(fetchEnvironmentsAction());

        const environments = await envRegService.fetchEnvironments();
        dispatch(fetchEnvironmentsSuccessAction(environments));
    } catch (err) {
        if (err instanceof ServiceAuthenticationError) {
            dispatch(logout({ isExplicit: false }));
            dispatch(fetchEnvironmentsFailureAction(err));

            throw err;
        }

        dispatch(fetchEnvironmentsFailureAction(err));
    }
}
