import { ILocalCloudEnvironment, StateInfo, EnvironmentType } from '../interfaces/cloudenvironment';
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
    pollActivatingEnvironmentsActionType,
    pollActivatingEnvironmentsUpdateActionType,
    PollActivatingEnvironmentsAction,
    PollActivatingEnvironmentsUpdateAction,
} from '../actions/pollEnvironment';
import { deleteEnvironmentActionType, DeleteEnvironmentAction } from '../actions/deleteEnvironment';
import {
    stateChangeEnvironmentActionType,
    StateChangeEnvironmentAction,
} from '../actions/environmentStateChange';
import { isActivating } from '../utils/environmentUtils';
import { ApplicationState } from './rootReducer';
import { selectPlanSuccessActionType, SelectPlanSuccessAction } from '../actions/plans-actions';

type EnvironmentsState = {
    environments: ILocalCloudEnvironment[];
    deletedEnvironments: ILocalCloudEnvironment[];
    activatingEnvironments: string[];
    selectedPlanId: string | null;
    isLoading: boolean;
};

type AcceptedActions =
    | FetchEnvironmentsAction
    | FetchEnvironmentsFailureAction
    | FetchEnvironmentsSuccessAction
    | CreateEnvironmentAction
    | CreateEnvironmentSuccessAction
    | CreateEnvironmentFailureAction
    | PollActivatingEnvironmentsAction
    | PollActivatingEnvironmentsUpdateAction
    | DeleteEnvironmentAction
    | StateChangeEnvironmentAction
    | SelectPlanSuccessAction;

const defaultState: EnvironmentsState = {
    environments: [] as ILocalCloudEnvironment[],
    deletedEnvironments: [] as ILocalCloudEnvironment[],
    activatingEnvironments: [] as string[],
    selectedPlanId: null,
    isLoading: true,
} as const;

// tslint:disable-next-line: max-func-body-length
export function environments(
    state: EnvironmentsState | undefined = defaultState,
    action: AcceptedActions
): EnvironmentsState {
    switch (action.type) {
        case selectPlanSuccessActionType:
            return ((state, action) => {
                const { plan } = action.payload;
                if (!plan) {
                    return {
                        ...state,
                        selectedPlanId: null,
                        activatingEnvironments: [],
                    };
                }

                const activatingEnvironments = state.environments.reduce(
                    (envs, env) => {
                        if (env.planId && plan.id !== env.planId) {
                            return envs;
                        }
                        if (env.id && isActivating(env)) {
                            envs.push(env.id);
                        }
                        return envs;
                    },
                    [] as string[]
                );

                return {
                    ...state,
                    selectedPlanId: plan.id,
                    activatingEnvironments,
                };
            })(state, action);

        case fetchEnvironmentsSuccessActionType:
            const activatingEnvironments = action.payload.environments.reduce(
                (envs, env) => {
                    if (state.selectedPlanId && state.selectedPlanId !== env.planId) {
                        return envs;
                    }

                    if (isActivating(env)) {
                        envs.push(env.id);
                    }
                    return envs;
                },
                [] as string[]
            );

            return {
                ...state,
                isLoading: false,
                environments: action.payload.environments,
                activatingEnvironments,
                deletedEnvironments: [],
            };
        case createEnvironmentActionType:
            const {
                type = EnvironmentType.CloudEnvironment,
                friendlyName,
                gitRepositoryUrl,
                dotfilesInstallCommand,
                dotfilesTargetPath,
                dotfilesRepository,
                autoShutdownDelayMinutes,
                skuName,
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
                autoShutdownDelayMinutes,
                skuName,
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
                    activatingEnvironments: [...state.activatingEnvironments, environment.id],
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
                    activatingEnvironments: state.activatingEnvironments.filter(
                        (eId) => eId !== id
                    ),
                };
            })(state, action);

        case stateChangeEnvironmentActionType:
            return ((state, action) => {
                let { activatingEnvironments } = state;
                const { id, environmentState } = action.payload;
                const index = state.environments.findIndex((e) => e.id === id);

                if (index < 0) {
                    throw new Error(`${action.type} returned an environment we are not tracking.`);
                }

                const environment = {
                    ...state.environments[index],
                    state: environmentState,
                };

                if (isActivating({ state: environmentState })) {
                    activatingEnvironments = activatingEnvironments.filter((eId) => eId !== id);
                    activatingEnvironments.push(id);
                }

                return {
                    ...state,
                    environments: replaceAtIndex(state.environments, index, environment),
                    activatingEnvironments,
                };
            })(state, action);

        case pollActivatingEnvironmentsActionType:
            return ((state, action) => {
                const { id } = action.payload;
                let { activatingEnvironments } = state;

                activatingEnvironments = activatingEnvironments.filter((eId) => eId !== id);

                return {
                    ...state,
                    activatingEnvironments,
                };
            })(state, action);

        case pollActivatingEnvironmentsUpdateActionType:
            return ((state, action) => {
                const { id } = action.payload;
                let { activatingEnvironments } = state;

                activatingEnvironments = activatingEnvironments.filter((eId) => eId !== id);
                activatingEnvironments.push(id);

                return {
                    ...state,
                    activatingEnvironments,
                };
            })(state, action);

        default:
            return state;
    }
}
