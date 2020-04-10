import { IEnvironment } from 'vso-client-core';

import { action } from './middleware/useActionCreator';
import { useDispatch } from './middleware/useDispatch';
import { useActionContext } from './middleware/useActionContext';
import { useWebClient } from './middleware/useWebClient';
import {
    EnvironmentSettingsAllowedUpdates,
    EnvironmentSettingsUpdate,
} from '../interfaces/cloudenvironment';

export const getAllowedEnvironmentSettingsChangesActionType =
    'async.environments.getAllowedEnvironmentSettingsChanges';
export const getAllowedEnvironmentSettingsChangesSuccessActionType =
    'async.environments.getAllowedEnvironmentSettingsChanges.success';
export const getAllowedEnvironmentSettingsChangesFailureActionType =
    'async.environments.getAllowedEnvironmentSettingsChanges.failure';

const getAllowedEnvironmentSettingsChangesAction = (envId: string) =>
    action(getAllowedEnvironmentSettingsChangesActionType, { envId });
const getAllowedEnvironmentSettingsChangesSuccessAction = (
    envId: string,
    allowedUpdates: EnvironmentSettingsAllowedUpdates
) => action(getAllowedEnvironmentSettingsChangesSuccessActionType, { envId, allowedUpdates });
const getAllowedEnvironmentSettingsChangesFailureAction = (error: Error) =>
    action(getAllowedEnvironmentSettingsChangesFailureActionType, {}, error);

export type GetAllowedEnvironmentSettingsChangesAction = ReturnType<
    typeof getAllowedEnvironmentSettingsChangesAction
>;
export type GetAllowedEnvironmentSettingsChangesSuccessAction = ReturnType<
    typeof getAllowedEnvironmentSettingsChangesSuccessAction
>;
export type GetAllowedEnvironmentSettingsChangesFailureAction = ReturnType<
    typeof getAllowedEnvironmentSettingsChangesFailureAction
>;

export const updateEnvironmentSettingsActionType = 'async.environments.updateEnvironmentSettings';
export const updateEnvironmentSettingsSuccessActionType =
    'async.environments.updateEnvironmentSettings.success';
export const updateEnvironmentSettingsFailureActionType =
    'async.environments.updateEnvironmentSettings.failure';

const updateEnvironmentSettingsAction = (envId: string, update: EnvironmentSettingsUpdate) =>
    action(updateEnvironmentSettingsActionType, { envId, update });
const updateEnvironmentSettingsSuccessAction = (
    envId: string,
    update: EnvironmentSettingsUpdate,
    env: IEnvironment
) => action(updateEnvironmentSettingsSuccessActionType, { envId, update, env });
const updateEnvironmentSettingsFailureAction = (
    envId: string,
    update: EnvironmentSettingsUpdate,
    error: Error
) => action(updateEnvironmentSettingsFailureActionType, { envId, update }, error);

export type UpdateEnvironmentSettingsAction = ReturnType<typeof updateEnvironmentSettingsAction>;
export type UpdateEnvironmentSettingsSuccessAction = ReturnType<
    typeof updateEnvironmentSettingsSuccessAction
>;
export type UpdateEnvironmentSettingsFailureAction = ReturnType<
    typeof updateEnvironmentSettingsFailureAction
>;

export async function getAllowedEnvironmentSettingsChanges(
    environmentId: string
): Promise<EnvironmentSettingsAllowedUpdates> {
    const dispatch = useDispatch();

    try {
        dispatch(getAllowedEnvironmentSettingsChangesAction(environmentId));

        const actionContext = useActionContext();

        const { configuration } = actionContext.state;

        if (!configuration) {
            throw new Error('No configuration set, aborting.');
        }

        const { apiEndpoint } = configuration;

        const webClient = useWebClient();
        const url = new URL(`${apiEndpoint}/environments/${environmentId}/updates`);
        const response = await webClient.get<EnvironmentSettingsAllowedUpdates>(url.toString(), {
            retryCount: 2,
        });

        dispatch(getAllowedEnvironmentSettingsChangesSuccessAction(environmentId, response));
        return response;
    } catch (err) {
        return dispatch(getAllowedEnvironmentSettingsChangesFailureAction(err));
    }
}

export async function updateEnvironmentSettings(
    environmentId: string,
    update: EnvironmentSettingsUpdate
): Promise<IEnvironment> {
    const dispatch = useDispatch();

    try {
        dispatch(updateEnvironmentSettingsAction(environmentId, update));

        const actionContext = useActionContext();

        const { configuration } = actionContext.state;

        if (!configuration) {
            throw new Error('No configuration set, aborting.');
        }

        const { apiEndpoint } = configuration;

        const webClient = useWebClient();
        const url = new URL(`${apiEndpoint}/environments/${environmentId}`);
        const response = await webClient.patch<IEnvironment>(url.toString(), update, {
            retryCount: 2,
        });

        dispatch(updateEnvironmentSettingsSuccessAction(environmentId, update, response));
        return response;
    } catch (err) {
        return dispatch(updateEnvironmentSettingsFailureAction(environmentId, update, err));
    }
}
