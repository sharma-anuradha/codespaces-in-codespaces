import { CodespaceInitializationTrackerBase } from './CodespaceInitializationTrackerBase';
import { CodespaceInitializationTrackerCookie } from './CodespaceInitializationTrackerCookie';
import { CodespaceInitializationTrackerLocalStorage } from './CodespaceInitializationTrackerLocalStorage';

class CodespaceInitializationTracker extends CodespaceInitializationTrackerBase {
    constructor(private sources: CodespaceInitializationTrackerBase[]) {
        super();
    }

    public async markCodespaceAsFresh(): Promise<void> {
        const promises = this.sources.map((source) => {
            return source.markCodespaceAsFresh();
        });

        await Promise.all(promises);
    }

    public async markCodespaceAsUsed(): Promise<void> {
        const promises = this.sources.map((source) => {
            return source.markCodespaceAsUsed();
        });

        await Promise.all(promises);
    }

    public async isFirstCodespaceLoad(): Promise<boolean> {
        const promises = this.sources.map((source) => {
            return source.isFirstCodespaceLoad();
        });

        const result = await Promise.all(promises);

        return !result.includes(true);
    }
}

const sources = [
    new CodespaceInitializationTrackerLocalStorage(),
    new CodespaceInitializationTrackerCookie(),
];

export const codespaceInitializationTracker = new CodespaceInitializationTracker(sources);
