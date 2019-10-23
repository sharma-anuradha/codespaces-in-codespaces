import { getServiceConfiguration, IConfiguration } from '../services/configurationService';

import { action } from './middleware/useActionCreator';
import { useDispatch } from './middleware/useDispatch';

export const fetchConfigurationActionType = 'async.configuration.fetch';
export const fetchConfigurationSuccessActionType = 'async.configuration.fetch.success';
export const fetchConfigurationFailureActionType = 'async.configuration.fetch.failure';

// Basic actions dispatched for reducers
const fetchConfigurationAction = () => action(fetchConfigurationActionType);
const fetchConfigurationSuccessAction = (configuration: IConfiguration) =>
    action(fetchConfigurationSuccessActionType, { configuration });
const fetchConfigurationFailureAction = (error: Error) =>
    action(fetchConfigurationFailureActionType, error);

// Types to register with reducers
export type FetchConfigurationAction = ReturnType<typeof fetchConfigurationAction>;
export type FetchConfigurationSuccessAction = ReturnType<typeof fetchConfigurationSuccessAction>;
export type FetchConfigurationFailureAction = ReturnType<typeof fetchConfigurationFailureAction>;

// Exposed - callable actions that have side-effects
export async function fetchConfiguration() {
    const dispatch = useDispatch();

    try {
        dispatch(fetchConfigurationAction());
        const configuration = await getServiceConfiguration();

        dispatch(fetchConfigurationSuccessAction(configuration));

        return configuration;
    } catch (err) {
        return dispatch(fetchConfigurationFailureAction(err));
    }
}
