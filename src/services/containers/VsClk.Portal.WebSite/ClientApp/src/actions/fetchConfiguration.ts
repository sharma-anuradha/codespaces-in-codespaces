import { getServiceConfiguration, IConfiguration } from '../services/configurationService';

import { action } from './middleware/useActionCreator';
import { useDispatch } from './middleware/useDispatch';
import { postServiceWorkerMessage } from '../common/post-message';
import { configureServiceWorker } from '../common/service-worker-messages';
import { VSLS_API_URI } from '../constants';

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

        // TODO: #984591 Configure live share endpoint from configuration controller.
        postServiceWorkerMessage({
            type: configureServiceWorker,
            payload: {
                liveShareEndpoint: VSLS_API_URI,
            },
        });

        dispatch(fetchConfigurationSuccessAction(configuration));
    } catch (err) {
        dispatch(fetchConfigurationFailureAction(err));
    }
}
