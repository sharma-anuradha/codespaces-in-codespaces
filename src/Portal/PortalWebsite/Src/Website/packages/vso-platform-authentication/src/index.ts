import {
    localStorageKeychain,
    createKeys,
    setKeychainKeys,
    PARTNER_INFO_KEYCHAIN_KEY,
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
        
        const params = new URLSearchParams(window.location.search);

        const redirectParam = params.get('redirect') || '/';
        const relativeRedirect = new URL(redirectParam, location.href).pathname;

        // if `redirect` param was set to non-empty string(multiple spaces or non-path string),
        // the parsing logic above will result the relativeRedirect to be the current path
        location.href = (relativeRedirect === location.pathname)
            ? '/'
            : relativeRedirect;
    } catch (e) {
        throw e;
    }
})();
