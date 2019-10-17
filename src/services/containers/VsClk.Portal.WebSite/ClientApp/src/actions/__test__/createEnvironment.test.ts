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

        await store.dispatch(
            createEnvironment({
                friendlyName: 'test',
                accountId: '/subscriptions/mockSubscription/resourceGroups/mockResourceGroup/providers/Microsoft.VSOnline/accounts/test',
                location: 'WestUS2',
                autoShutdownDelayMinutes: 30
            })
        );

        expect(store.dispatchedActions).not.toHaveFailed();
        expect(store.dispatchedActions).toHaveBeenDispatchedInOrder(
            createEnvironmentActionType,
            createEnvironmentSuccessActionType,
            'mock.poll'
        );
        const successAction = getDispatchedAction(
            store.dispatchedActions,
            createEnvironmentSuccessActionType
        );
        expect(successAction.payload.environment).toBe(createdEnvironment);
    });
});
