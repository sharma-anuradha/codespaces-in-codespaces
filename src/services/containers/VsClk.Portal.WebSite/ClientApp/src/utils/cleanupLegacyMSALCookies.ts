import { getAllCookies } from "./getCookies";
import { deleteCookie } from "./deleteCookie";

const MSAL_COOKIE_PREFIX = 'msal.authority|';

export const cleanupLegacyMSALCookies = () => {
    const cookies = getAllCookies();
    
    for (let cookie of cookies) {
        if (cookie.name.startsWith(MSAL_COOKIE_PREFIX)) {
            deleteCookie(cookie.name);
        }
    }
}