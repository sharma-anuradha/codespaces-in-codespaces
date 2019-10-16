import { ILocalCloudEnvironment, StateInfo } from '../interfaces/cloudenvironment';
import { replaceAtIndex } from './reducerUtils';

import {
    fetchEnvironmentsSuccessActionType,
    FetchEnvironmentsAction,
    FetchEnvironmentsFailureAction,
    FetchEnvironmentsSuccessAction,
} from '../actions/fetchEnvironments';

import {
    createEnvironmentActionType,
    createEnvironmentFailureActionType,
    createEnvironmentSuccessActionType,
    CreateEnvironmentAction,
    CreateEnvironmentSuccessAction,
    CreateEnvironmentFailureAction,
} from '../actions/createEnvironment';
import {
    pollEnvironmentUpdateActionType,
    pollEnvironmentSuccessActionType,
    PollEnvironmentSuccessAction,
    PollEnvironmentUpdateAction,
} from '../actions/pollEnvironment';
import { deleteEnvironmentActionType, DeleteEnvironmentAction } from '../actions/deleteEnvironment';
import { stateChangeEnvironmentActionType, StateChangeEnvironmentAction } from '../actions/environmentStateChange';

type EnvironmentsState = {
    environments: ILocalCloudEnvironment[];
    deletedEnvironments: ILocalCloudEnvironment[];
    isLoading: boolean;
};

type AcceptedActions =
    | FetchEnvironmentsAction
    | FetchEnvironmentsFailureAction
    | FetchEnvironmentsSuccessAction
    | CreateEnvironmentAction
    | CreateEnvironmentSuccessAction
    | CreateEnvironmentFailureAction
    | PollEnvironmentUpdateAction
    | PollEnvironmentSuccessAction
    | DeleteEnvironmentAction
    | StateChangeEnvironmentAction

const defaultState: EnvironmentsState = {
    environments: [] as ILocalCloudEnvironment[],
    deletedEnvironments: [] as ILocalCloudEnvironment[],
    isLoading: true,
} as const;

// tslint:disable-next-line: max-func-body-length
export function environments(
    state: EnvironmentsState | undefined = defaultState,
    action: AcceptedActions
): EnvironmentsState {
    switch (action.type) {
        case fetchEnvironmentsSuccessActionType:
            return {
                ...state,
                isLoading: false,
                environments: action.payload.environments,
                deletedEnvironments: [],
            };
        case createEnvironmentActionType:
            const {
                type = 'cloudEnvironment',
                friendlyName,
                gitRepositoryUrl,
                dotfilesInstallCommand,
                dotfilesTargetPath,
                dotfilesRepository,
            } = action.payload.environment;

            const envLie: ILocalCloudEnvironment = {
                type,
                friendlyName,
                created: new Date(),
                updated: new Date(),
                seed: {
                    moniker: gitRepositoryUrl || '',
                    type: gitRepositoryUrl ? 'git' : '',
                },
                personalization: {
                    dotfilesInstallCommand,
                    dotfilesTargetPath,
                    dotfilesRepository,
                },
                state: StateInfo.Provisioning,
                lieId: action.payload.lieId,
            };

            return {
                ...state,
                environments: [envLie, ...state.environments],
            };

        case createEnvironmentFailureActionType:
            return (() => {
                if (!action.payload) {
                    return state;
                }

                const { lieId } = action.payload;
                const failedIndex = state.environments.findIndex((e) => e.lieId === lieId);

                if (failedIndex < 0) {
                    throw new Error(`${action.type} returned an environment we are not tracking.`);
                }

                const failedCreation = state.environments[failedIndex];

                return {
                    ...state,
                    environments: replaceAtIndex(state.environments, failedIndex, {
                        ...failedCreation,
                        state: StateInfo.Failed,
                    }),
                };
            })();

        case createEnvironmentSuccessActionType:
            return ((state, action) => {
                const { lieId, environment } = action.payload;
                const index = state.environments.findIndex((e) => e.lieId === lieId);

                if (index < 0) {
                    throw new Error(`${action.type} returned an environment we are not tracking.`);
                }

                return {
                    ...state,
                    environments: replaceAtIndex(state.environments, index, environment),
                };
            })(state, action);

        case pollEnvironmentUpdateActionType:
            return ((state, action) => {
                const { environment } = action.payload;
                const index = state.environments.findIndex((e) => e.id === environment.id);

                if (index < 0) {
                    throw new Error(`${action.type} returned an environment we are not tracking.`);
                }

                return {
                    ...state,
                    environments: replaceAtIndex(state.environments, index, environment),
                };
            })(state, action);

        case pollEnvironmentSuccessActionType:
            return ((state, action) => {
                const { environment } = action.payload;
                const updatedIndex = state.environments.findIndex((e) => e.id === environment.id);

                if (updatedIndex < 0) {
                    throw new Error(`${action.type} returned an environment we are not tracking.`);
                }

                return {
                    ...state,
                    environments: replaceAtIndex(state.environments, updatedIndex, environment),
                };
            })(state, action);

        case deleteEnvironmentActionType:
            return ((state, action) => {
                const { id } = action.payload;

                const environmentToDelete = state.environments.filter((e) => e.id === id);
                const restOfEnvironments = state.environments.filter((e) => e.id !== id);

                return {
                    ...state,
                    environments: restOfEnvironments,
                    deletedEnvironments: [...environmentToDelete, ...state.deletedEnvironments],
                };
            })(state, action);

        case stateChangeEnvironmentActionType:
            return ((state, action) => {
                const { id, environmentState } = action.payload;
                const index = state.environments.findIndex((e) => e.id === id);

                if (index < 0) {
                    throw new Error(`${action.type} returned an environment we are not tracking.`);
                }

                var environment = state.environments[index];
                environment.state = environmentState;
                return {
                    ...state,
                    environments: replaceAtIndex(state.environments, index, environment),
                };
            })(state, action);

        default:
            return state;
    }
}
