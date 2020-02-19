import { action } from './middleware/useActionCreator';

import { authService } from '../services/authService';
import { useDispatch } from './middleware/useDispatch';

import { INDEXEDDB_VSONLINE_DB, deleteDatabase as deleteIndexedDb } from '../utils/indexedDBFS';
import { deleteAuthCookie } from '../utils/setAuthCookie';

export const logoutActionType = 'async.authentication.clearData';

// Basic actions dispatched for reducers
const logoutAction = () => action(logoutActionType);

// Types to register with reducers
export type LogoutAction = ReturnType<typeof logoutAction>;

// Exposed - callable actions that have side-effects
export async function logout() {
    const dispatch = useDispatch();
    dispatch(logoutAction());

    // clear indexedDb
    try {
        await deleteIndexedDb(INDEXEDDB_VSONLINE_DB);
    } catch {}

    // clear storage
    try {
        localStorage.clear();
    } catch {}

    try {
        sessionStorage.clear();
    } catch {}

    // clear msa aad
    try {
        await authService.logout();
    } catch {}

    // clear cookie and auth cookie
    // tslint:disable: no-cookies
    var cookies = document.cookie.split(';');
    for (var i = 0; i < cookies.length; i++) {
        const cookieName = cookies[i].split('=')[0];
        document.cookie = cookieName + '=;expires=Thu, 21 Sep 1979 00:00:01 UTC;';
    }
    // tslint:enable: no-cookies

    await deleteAuthCookie();
}
