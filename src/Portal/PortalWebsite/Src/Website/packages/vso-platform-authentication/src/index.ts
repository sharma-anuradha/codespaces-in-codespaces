import {
    localStorageKeychain,
    createKeys,
    setKeychainKeys,
    PARTNER_INFO_KEYCHAIN_KEY,
    VSCS_LOADING_SCREEN_THEME_COLOR_LS_KEY,
    VSCS_LOADING_SCREEN_FAVICON_LS_KEY,
} from 'vso-client-core';

(async () => {
    /**
     * TODO: render error page for all errors and redirect to the correct place.
     */

    const dataEl = document.querySelector('#js-partner-info');
    if (!dataEl) {
        throw new Error('No data element found.');
    }

    const { textContent } = dataEl;
    if (!textContent) {
        throw new Error('No data element found.');
    }

    try {
        const data = JSON.parse(atob(textContent));

        const keys = await createKeys(data.codespaceToken);
        if (!keys.length) {
            throw new Error('Cannot create encryption keys.');
        }

        setKeychainKeys(keys);
        await localStorageKeychain.set(PARTNER_INFO_KEYCHAIN_KEY, JSON.stringify(data));

        // copy over the loadingScreenThemeColor that set on the `connect` page to allow
        // workbench page to use the same color thus prevent flickering
        const { loadingScreenThemeColor } = data.vscodeSettings || {};
        localStorage.setItem(VSCS_LOADING_SCREEN_THEME_COLOR_LS_KEY, loadingScreenThemeColor);

        // copy over the favicon that set on the `connect` page to allow
        // workbench page to use the same icon thus prevent flickering
        const faviconEl = document.querySelector('#js-favicon');
        if (faviconEl) {
            localStorage.setItem(VSCS_LOADING_SCREEN_FAVICON_LS_KEY, faviconEl.getAttribute('href') || '');
        }

        const params = new URLSearchParams(window.location.search);
        const redirectParam = params.get('redirect') || '/';
        const relativeRedirect = new URL(redirectParam, location.href).pathname;

        // if `redirect` query param was set to non-empty string(multiple spaces or non-path string),
        // the parsing logic above will result the `relativeRedirect` to be the current path
        const redirect = (relativeRedirect === location.pathname)
            ? '/'
            : relativeRedirect;

        if (typeof location.replace === 'function') {
            location.replace(redirect);
        } else {
            location.href = redirect;
        }
    } catch (e) {
        throw e;
    }
})();
