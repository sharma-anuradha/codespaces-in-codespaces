import { getServiceConfiguration, IConfiguration } from '../services/configurationService';

import { action, Dispatch } from './actionUtils';

export const fetchConfigurationActionType = 'async.configuration.fetch';
export const fetchConfigurationSuccessActionType = 'async.configuration.fetch.success';
export const fetchConfigurationFailureActionType = 'async.configuration.fetch.failure';

// Basic actions dispatched for reducers
const fetchConfigurationAction = () => action(fetchConfigurationActionType);
const fetchConfigurationSuccessAction = (configuration: IConfiguration) =>
    action(fetchConfigurationSuccessActionType, { configuration });
const fetchConfigurationFailureAction = (error: Error) =>
    action(fetchConfigurationFailureActionType, undefined, error);

// Types to register with reducers
export type FetchConfigurationAction = ReturnType<typeof fetchConfigurationAction>;
export type FetchConfigurationSuccessAction = ReturnType<typeof fetchConfigurationSuccessAction>;
export type FetchConfigurationFailureAction = ReturnType<typeof fetchConfigurationFailureAction>;

// Exposed - callable actions that have side-effects
export const fetchConfiguration = () => async (dispatch: Dispatch) => {
    try {
        dispatch(fetchConfigurationAction());
        const configuration = await getServiceConfiguration();
        dispatch(fetchConfigurationSuccessAction(configuration));
    } catch (err) {
        dispatch(fetchConfigurationFailureAction(err));
    }
};
