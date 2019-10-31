import {
    createMockStore,
    MockStore,
    test_setMockRequestFactory,
    createMockMakeRequestFactory,
    test_setApplicationState,
    authenticated,
} from '../../utils/testUtils';

import { getUserInfo, getUserInfoActionType, getUserInfoSuccessActionType } from '../getUserInfo';

import { defaultConfig } from '../../services/configurationService';
import * as acquireTokenModule from '../../services/acquireToken';

describe('getUserInfo', () => {
    let store: MockStore;

    beforeEach(() => {
        store = createMockStore();
        // in tests URL.createObjectURL is not a function
        URL.createObjectURL = () => {
            return '';
        };
    });
    afterEach(() => {
        URL.createObjectURL = undefined!;
    });

    it('gets user info from graph', async () => {
        jest.spyOn(acquireTokenModule, 'acquireToken').mockReturnValue(Promise.resolve(authenticated.token));

        test_setMockRequestFactory(
            createMockMakeRequestFactory({
                responses: [
                    {
                        body: defaultConfig,
                    },
                    {
                        body: new Blob(['']),
                    },
                ],
            })
        );
        test_setApplicationState({
            authentication: authenticated,
        });

        await store.dispatch(getUserInfo());

        expect(store.dispatchedActions).not.toHaveFailed();
        expect(store.dispatchedActions).toHaveBeenDispatchedInOrder(
            getUserInfoActionType,
            getUserInfoSuccessActionType
        );
    });

    it('uses the user info from app state', async () => {
        const userInfo = {
            mail: 'test@test.com',
            displayName: 'test',
            photoUrl: 'https://test.com',
        };
        test_setApplicationState({
            userInfo,
        });

        const state = await store.dispatch(getUserInfo());

        expect(state).toBe(userInfo);
    });
});
