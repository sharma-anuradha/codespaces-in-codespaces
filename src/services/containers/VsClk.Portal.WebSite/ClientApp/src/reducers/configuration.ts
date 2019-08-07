import { IConfiguration } from '../services/configurationService';

import {
    fetchConfigurationSuccessActionType,
    FetchConfigurationAction,
    FetchConfigurationFailureAction,
    FetchConfigurationSuccessAction,
} from '../actions/configuration';

type ConfigurationState = IConfiguration | null;
type AcceptedActions =
    | FetchConfigurationAction
    | FetchConfigurationFailureAction
    | FetchConfigurationSuccessAction;

export function configuration(
    state: ConfigurationState | undefined = null,
    action: AcceptedActions
): ConfigurationState {
    switch (action.type) {
        case fetchConfigurationSuccessActionType:
            return action.payload.configuration;
        default:
            return state;
    }
}
