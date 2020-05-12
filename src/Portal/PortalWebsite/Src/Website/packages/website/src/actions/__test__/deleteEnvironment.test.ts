import {
    createMockStore,
    MockStore,
    test_setMockRequestFactory,
    createMockMakeRequestFactory,
    test_setApplicationState,
    authenticated,
} from '../../utils/testUtils';

import {
    deleteEnvironment,
    deleteEnvironmentActionType,
    deleteEnvironmentSuccessActionType,
} from '../deleteEnvironment';
import { defaultConfig } from '../../services/configurationService';

jest.mock('../../services/authService', () => {
    return {
        authService: {
            getCachedToken: async () => {
                return 'AAD token value';
            },
        },
    };
});

describe('deleteEnvironment', () => {
    let store: MockStore;

    beforeEach(() => {
        store = createMockStore();
    });

    it('deletes environment', async () => {
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
            configuration: defaultConfig,
            authentication: authenticated,
        });

        await store.dispatch(deleteEnvironment('42'));

        expect(store.dispatchedActions).not.toHaveFailed();
        expect(store.dispatchedActions).toHaveBeenDispatchedInOrder(
            deleteEnvironmentActionType,
            deleteEnvironmentSuccessActionType
        );
    });
});
