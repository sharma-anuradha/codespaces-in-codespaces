import { action } from './middleware/useActionCreator';

import { authService } from '../services/authService';
import { useDispatch } from './middleware/useDispatch';

import { INDEXEDDB_VSONLINE_DB, deleteDatabase as deleteIndexedDb } from '../utils/indexedDBFS';

export const logoutActionType = 'async.authentication.clearData';
const exemptCookieList = ['MSCC'];
const exemptLocalStorageItems = [
    'azDevAccessTokenEncrypted',
    'githubAccessTokenEncrypted',
    'vso-featureset',
    'vscode.baseTheme',
];
const alwaysExemptLocalStorageItems = ['vso_machine_id', 'vso.marketing.blog.post.seen'];

// Basic actions dispatched for reducers
const logoutAction = () => action(logoutActionType);

interface LocalStorageItems {
    [key: string]: string;
}

// Types to register with reducers
export type LogoutAction = ReturnType<typeof logoutAction>;

// Exposed - callable actions that have side-effects
export async function logout(props: { isExplicit: boolean }) {
    const dispatch = useDispatch();
    dispatch(logoutAction());

    // clear indexedDb
    try {
        await deleteIndexedDb(INDEXEDDB_VSONLINE_DB);
    } catch {}

    // clear storage
    try {
        if (props.isExplicit) {
            clearLocalStorage(alwaysExemptLocalStorageItems);
        } else {
            clearLocalStorage([...alwaysExemptLocalStorageItems, ...exemptLocalStorageItems]);
        }
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
        if (!exemptCookieList.includes(cookieName.trim())) {
            document.cookie = cookieName + '=;expires=Thu, 21 Sep 1979 00:00:01 UTC;';
        }
    }
    // tslint:enable: no-cookies

    // TODO: Logout PF cookies we don't see and don't know about here and
    // won't see on server either as they are for unknown specific domains.
}

function clearLocalStorage(exemptKeys: string[]) {
    let keyValues: LocalStorageItems = {};
    for (var i = 0; i < localStorage.length; i++) {
        const key = localStorage.key(i);
        if (key && exemptKeys.includes(key)) {
            const value = localStorage.getItem(key);
            if (value) {
                keyValues[key] = value;
            }
        }
    }
    localStorage.clear();
    exemptKeys.forEach((key: string) => {
        const value = keyValues[key];
        if (value) {
            localStorage.setItem(key, value);
        }
    });
}
