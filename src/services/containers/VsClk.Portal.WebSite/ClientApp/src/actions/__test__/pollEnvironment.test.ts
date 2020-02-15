import {
    createMockStore,
    MockStore,
    test_setMockRequestFactory,
    createMockMakeRequestFactory,
    test_setApplicationState,
    authenticated,
    getDispatchedAction,
} from '../../utils/testUtils';

import { defaultConfig } from '../../services/configurationService';
import { StateInfo, ILocalCloudEnvironment } from '../../interfaces/cloudenvironment';
import { stateChangeEnvironmentActionType } from '../environmentStateChange';
import {
    pollActivatingEnvironment,
    pollActivatingEnvironmentsActionType,
} from '../pollEnvironment';
import { environmentChangedActionType } from '../environmentChanged';
import { EnvironmentsState } from '../../reducers/environments';

jest.mock('../../services/authService', () => {
    return {
        authService: {
            getCachedToken: async () => {
                return 'AAD token value';
            },
        },
    };
});

describe('pollEnvironment', () => {
    let store: MockStore;
    const environmentInState = {
        id: 'env-id',
        state: StateInfo.Available,
    };

    const environmentFromService = {
        id: environmentInState.id,
        state: StateInfo.ShuttingDown,
    };

    beforeEach(() => {
        store = createMockStore({
            authentication: authenticated,
            configuration: defaultConfig,
            environments: {
                environments: [environmentInState] as ILocalCloudEnvironment[],
                activatingEnvironments: [] as string[],
            } as EnvironmentsState,
        });
    });

    it('StateChange action occured during polling environments', async () => {
        test_setMockRequestFactory(
            createMockMakeRequestFactory({
                responses: [
                    {
                        body: environmentFromService,
                    },
                ],
            })
        );

        test_setApplicationState({
            authentication: authenticated,
            configuration: defaultConfig,
        });

        await store.dispatch(pollActivatingEnvironment(environmentInState.id));

        const pollAction = getDispatchedAction(
            store.dispatchedActions,
            pollActivatingEnvironmentsActionType
        );
        const stateChangeAction = getDispatchedAction(
            store.dispatchedActions,
            stateChangeEnvironmentActionType
        );
        const changeEnvAction = getDispatchedAction(
            store.dispatchedActions,
            environmentChangedActionType
        );

        expect(pollAction.metadata.correlationId).toBe(stateChangeAction.metadata.correlationId);
        expect(stateChangeAction.metadata.correlationId).toBe(
            changeEnvAction.metadata.correlationId
        );
        expect(stateChangeAction.metadata.correlationId).toBe(
            changeEnvAction.metadata.correlationId
        );
        expect(stateChangeAction.metadata.telemetryProperties['action.context.state']).toBe(
            environmentFromService.state
        );
        expect(stateChangeAction.metadata.telemetryProperties['action.context.oldState']).toBe(
            environmentInState.state
        );
        expect(stateChangeAction.metadata.telemetryProperties['action.context.environmentid']).toBe(
            environmentFromService.id
        );
    });

    it('StateChange action does not occured during polling environments', async () => {
        test_setMockRequestFactory(
            createMockMakeRequestFactory({
                responses: [
                    {
                        body: environmentFromService,
                    },
                ],
            })
        );

        test_setApplicationState({
            authentication: authenticated,
            configuration: defaultConfig,
        });

        await store.dispatch(pollActivatingEnvironment(environmentInState.id));

        const stateChangeAction = getDispatchedAction(
            store.dispatchedActions,
            stateChangeEnvironmentActionType
        );

        expect(stateChangeAction).not.toBeDefined;
    });
});
