import { createMockStore, MockStore } from '../../utils/testUtils';

import { logout, logoutActionType } from '../logout';
import { authService } from '../../services/authService';

jest.mock('../../utils/indexedDBFS', () => ({
    deleteDatabase: jest.fn(),
}));

jest.mock('../../utils/setAuthCookie', () => ({
    deleteAuthCookie: jest.fn(),
}));

describe('logout', () => {
    let store: MockStore;

    beforeEach(() => {
        store = createMockStore();
    });

    it('signs user out', async () => {
        const signout = jest.spyOn(authService, 'logout');
        await store.dispatch(logout({ isExplicit: true }));

        expect(store.dispatchedActions).toHaveBeenDispatchedInOrder(logoutActionType);
        expect(signout).toHaveBeenCalled();
    });
});
