import { createMockStore, MockStore } from '../../utils/testUtils';

import { clearAuthToken, clearAuthTokenActionType } from '../clearAuthToken';
import { authService } from '../../services/authService';

describe('clearAuthToken', () => {
    let store: MockStore;

    beforeEach(() => {
        store = createMockStore();
    });

    it('signs user out', async () => {
        const logout = jest.spyOn(authService, 'logout');
        await store.dispatch(clearAuthToken());

        expect(store.dispatchedActions).toHaveBeenDispatchedInOrder(clearAuthTokenActionType);
        expect(logout).toHaveBeenCalled();
    });
});
