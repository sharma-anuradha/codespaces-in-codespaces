import { createMockStore, MockStore } from '../../utils/testUtils';

import { clearAuthToken, clearAuthTokenActionType } from '../clearAuthToken';
import { authService } from '../../services/authService';

describe('clearAuthToken', () => {
    let store: MockStore;

    beforeEach(() => {
        store = createMockStore();
    });

    it('signs user out', async () => {
        const signOut = jest.spyOn(authService, 'signOut');
        await store.dispatch(clearAuthToken());

        expect(store.dispatchedActions).toHaveBeenDispatchedInOrder(clearAuthTokenActionType);
        expect(signOut).toHaveBeenCalled();
    });
});
