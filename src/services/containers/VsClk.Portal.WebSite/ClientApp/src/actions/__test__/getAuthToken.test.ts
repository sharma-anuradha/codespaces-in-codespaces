import { createMockStore, MockStore } from '../../utils/testUtils';

import {
    getAuthTokenActionType,
    getAuthTokenSuccessActionType,
} from '../getAuthTokenActions';
import { getAuthToken } from '../getAuthToken';
import { authService } from '../../services/authService';

describe('fetchConfiguration', () => {
    let store: MockStore;

    beforeEach(() => {
        store = createMockStore();
    });

    it('sets auth token', async () => {
        const logout = jest
            .spyOn(authService, 'getCachedToken')
            .mockReturnValue(
                Promise.resolve({
                    accessToken: 'token',
                    expiresOn: new Date(),
                    account: undefined!,
                })
            );
        await store.dispatch(getAuthToken());

        expect(store.dispatchedActions).not.toHaveFailed();

        expect(store.dispatchedActions).toHaveBeenDispatchedInOrder(
            getAuthTokenActionType,
            getAuthTokenSuccessActionType
        );
        expect(logout).toHaveBeenCalled();
    });
});
