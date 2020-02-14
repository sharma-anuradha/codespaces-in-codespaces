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

    beforeEach(() => {
        store = createMockStore({
            authentication: authenticated,
            configuration: defaultConfig,
            environments: {
                environments: [
                    {
                        id: 'env-id',
                        state: StateInfo.Available,
                    },
                ] as ILocalCloudEnvironment[],
                activatingEnvironments: [] as string[],
            } as EnvironmentsState,
        });
    });

    it('StateChange action occured during polling environments', async () => {
        test_setMockRequestFactory(
            createMockMakeRequestFactory({
                responses: [
                    {
                        body: {
                            id: 'env-id',
                            state: StateInfo.ShuttingDown,
                        },
                    },
                ],
            })
        );

        test_setApplicationState({
            authentication: authenticated,
            configuration: defaultConfig,
        });

        await store.dispatch(pollActivatingEnvironment('env-id'));

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
    });

    it('StateChange action does not occured during polling environments', async () => {
        test_setMockRequestFactory(
            createMockMakeRequestFactory({
                responses: [
                    {
                        body: {
                            id: 'env-id',
                            state: StateInfo.Available,
                        },
                    },
                ],
            })
        );

        test_setApplicationState({
            authentication: authenticated,
            configuration: defaultConfig,
        });

        await store.dispatch(pollActivatingEnvironment('env-id'));

        const stateChangeAction = getDispatchedAction(
            store.dispatchedActions,
            stateChangeEnvironmentActionType
        );

        expect(stateChangeAction).not.toBeDefined;
    });
});
