import {
    createMockStore,
    MockStore,
    test_setMockRequestFactory,
    createMockMakeRequestFactory,
    test_setApplicationState,
    authenticated,
    getDispatchedAction,
} from '../../utils/testUtils';

import {
    createEnvironment,
    createEnvironmentActionType,
    createEnvironmentSuccessActionType,
} from '../createEnvironment';

import { defaultConfig } from '../../services/configurationService';

jest.mock('../pollEnvironment', () => {
    return { pollEnvironment: jest.fn().mockReturnValue({ type: 'mock.poll' }) };
});
jest.mock('../getUserInfo', () => {
    return {
        getUserInfo: jest.fn().mockReturnValue({ mail: 'test@test.com', displayName: 'test' }),
    };
});

describe('createEnvironment', () => {
    let store: MockStore;

    beforeEach(() => {
        store = createMockStore();
    });

    it('creates new environment', async () => {
        const createdEnvironment = {
            id: '42',
            friendlyName: 'test',
            planId: 'test',
            location: 'WestUS2',
            autoShutdownDelayMinutes: 30,
            skuName: 'standardLinux',
        };

        test_setMockRequestFactory(
            createMockMakeRequestFactory({
                responses: [
                    {
                        body: createdEnvironment,
                    },
                ],
            })
        );
        test_setApplicationState({
            authentication: authenticated,
            configuration: defaultConfig,
        });

        const createEnvironmentRequest = {
            friendlyName: 'test',
            planId: '/subscriptions/mockSubscription/resourceGroups/mockResourceGroup/providers/Microsoft.VSOnline/plans/test',
            location: 'WestUS2',
            autoShutdownDelayMinutes: 30,
            skuName: 'standardLinux'
        };

        await store.dispatch(
            createEnvironment(createEnvironmentRequest)
        );

        expect(store.dispatchedActions).not.toHaveFailed();
        expect(store.dispatchedActions).toHaveBeenDispatchedInOrder(
            createEnvironmentActionType,
            createEnvironmentSuccessActionType,
            'mock.poll'
        );

        const createAction = getDispatchedAction(
            store.dispatchedActions,
            createEnvironmentActionType
        );
        expect(createAction.payload.environment).toBe(createEnvironmentRequest);

        const successAction = getDispatchedAction(
            store.dispatchedActions,
            createEnvironmentSuccessActionType
        );
        expect(successAction.payload.environment).toBe(createdEnvironment);
    });
});
