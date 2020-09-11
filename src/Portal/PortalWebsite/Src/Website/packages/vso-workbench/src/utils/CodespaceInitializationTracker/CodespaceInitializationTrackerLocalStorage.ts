import {
    LOAD_FLAG_KEY,
    LOAD_FLAG_VALUE,
    CodespaceInitializationTrackerBase
} from './CodespaceInitializationTrackerBase';

export class CodespaceInitializationTrackerLocalStorage extends CodespaceInitializationTrackerBase {
    public async markCodespaceAsFresh(): Promise<void> {
        localStorage.removeItem(LOAD_FLAG_KEY);
    }

    public async markCodespaceAsUsed(): Promise<void> {
        localStorage.setItem(LOAD_FLAG_KEY, LOAD_FLAG_VALUE);
    }

    public async isFirstCodespaceLoad(): Promise<boolean> {
        return localStorage.getItem(LOAD_FLAG_KEY) === LOAD_FLAG_VALUE;
    }
}
