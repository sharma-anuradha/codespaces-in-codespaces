import { action } from './middleware/useActionCreator';

import { authService } from '../services/authService';
import { useDispatch } from './middleware/useDispatch';

import { INDEXEDDB_VSONLINE_DB, deleteDatabase as deleteIndexedDb } from '../utils/indexedDBFS';

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
}
