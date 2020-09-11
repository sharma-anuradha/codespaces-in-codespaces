import { cookies, timeConstants } from 'vso-client-core';

import {
    LOAD_FLAG_KEY,
    LOAD_FLAG_VALUE,
    CodespaceInitializationTrackerBase
} from './CodespaceInitializationTrackerBase';

export class CodespaceInitializationTrackerCookie extends CodespaceInitializationTrackerBase {
    public async markCodespaceAsFresh(): Promise<void> {
        cookies.setCookie(LOAD_FLAG_KEY, '', 0);
    }

    public async markCodespaceAsUsed(): Promise<void> {
        cookies.setCookie(LOAD_FLAG_KEY, LOAD_FLAG_VALUE, 5 * 365 * timeConstants.DAY_MS);
    }

    public async isFirstCodespaceLoad(): Promise<boolean> {
        return cookies.getCookie(LOAD_FLAG_KEY) === LOAD_FLAG_VALUE;
    }
}
