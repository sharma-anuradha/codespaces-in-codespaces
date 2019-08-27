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
    CreateEnvironmentSuccessAction,
} from '../createEnvironment';
import { defaultConfig } from '../../services/configurationService';

jest.mock('../pollEnvironment', () => {
    return { pollEnvironment: jest.fn().mockReturnValue({ type: 'mock.poll' }) };
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
