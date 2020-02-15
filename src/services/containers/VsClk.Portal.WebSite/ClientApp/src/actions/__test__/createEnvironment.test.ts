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
    createEnvironmentFailureActionType,
} from '../createEnvironment';

import { defaultConfig } from '../../services/configurationService';
import { ServiceResponseError } from '../middleware/useWebClient';
import { environmentErrorCodeToString } from '../../utils/environmentUtils';

jest.mock('../../services/authService', () => {
    return {
        authService: {
            getCachedToken: async () => {
                return 'AAD token value';
            },
        },
    };
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
            planId:
                '/subscriptions/mockSubscription/resourceGroups/mockResourceGroup/providers/Microsoft.VSOnline/plans/test',
            location: 'WestUS2',
            autoShutdownDelayMinutes: 30,
            skuName: 'standardLinux',
        };

        await store.dispatch(createEnvironment(createEnvironmentRequest));

        expect(store.dispatchedActions).not.toHaveFailed();
        expect(store.dispatchedActions).toHaveBeenDispatchedInOrder(
            createEnvironmentActionType,
            createEnvironmentSuccessActionType
        );

        const createAction = getDispatchedAction(
            store.dispatchedActions,
            createEnvironmentActionType
        );

        const successAction = getDispatchedAction(
            store.dispatchedActions,
            createEnvironmentSuccessActionType
        );

        const failAction = getDispatchedAction(
            store.dispatchedActions,
            createEnvironmentFailureActionType
        );

        expect(createAction.payload.environment).toBe(createEnvironmentRequest);
        expect(failAction).toBeUndefined();
        expect(successAction.payload.environment).toBe(createdEnvironment);
        expect(successAction.metadata.telemetryProperties['action.context.environmentid']).toBe(
            createdEnvironment.id
        );
    });

    it('fails with 503 and error status code', async () => {
        test_setMockRequestFactory(
            createMockMakeRequestFactory({
                responses: [
                    {
                        body: '4',
                        status: 503,
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
            planId:
                '/subscriptions/mockSubscription/resourceGroups/mockResourceGroup/providers/Microsoft.VSOnline/plans/test',
            location: 'WestUS2',
            autoShutdownDelayMinutes: 30,
            skuName: 'standardLinux',
        };

        try {
            await store.dispatch(createEnvironment(createEnvironmentRequest));
        } catch {
            const failAction = getDispatchedAction(
                store.dispatchedActions,
                createEnvironmentFailureActionType
            );
            const successAction = getDispatchedAction(
                store.dispatchedActions,
                createEnvironmentSuccessActionType
            );
            expect(successAction).toBeUndefined();
            expect(failAction.error).toBeInstanceOf(ServiceResponseError);
            expect(failAction.payload!.errorMessage).toBe(environmentErrorCodeToString(4));
            expect(store.dispatch);
            expect(store.dispatchedActions).toHaveFailed();
        }
    });
});
