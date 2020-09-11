
export const LOAD_FLAG_KEY = `codespaces-page-load-flag`;
export const LOAD_FLAG_VALUE = `loaded`;

export abstract class CodespaceInitializationTrackerBase {
    abstract async markCodespaceAsFresh(): Promise<void>;
    abstract async markCodespaceAsUsed(): Promise<void>;
    abstract async isFirstCodespaceLoad(): Promise<boolean>;
}
