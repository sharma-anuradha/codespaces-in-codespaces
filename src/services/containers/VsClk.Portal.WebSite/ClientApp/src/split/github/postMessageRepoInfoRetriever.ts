import { isGithubTLD } from '../../utils/isHostedOnGithub';
import { createUniqueId } from '../../dependencies';

export interface IRepoInfo {
    ownerUsername: string;
    workspaceId: string;
    repositoryId: string;
    environmentId: string;
}

export class PostMessageRepoInfoRetriever {
    private awaitResponsePromises: Map<string, [Function, Function]> = new Map();

    private localStorageKey: string;

    constructor(workspaceId: string) {
        this.localStorageKey = `vso-github-repo-info-${workspaceId}`;

        window.addEventListener('message', this.receiveMessage, false);
    }

    private isValidInfo = (info: IRepoInfo) => {
        return info.ownerUsername && info.workspaceId && info.repositoryId;
    };

    private getStoredInfo() {
        const storedInfoString = localStorage.getItem(this.localStorageKey);
        if (storedInfoString) {
            try {
                const storedInfo = JSON.parse(storedInfoString) as IRepoInfo;

                if (this.isValidInfo(storedInfo)) {
                    return storedInfo;
                }
            } catch {}
        }
    }

    public getStoredRepoInfo = (id = createUniqueId()): IRepoInfo | undefined => {
        const storedInfo = this.getStoredInfo();
        return storedInfo;
    };

    public getRepoInfo = async (id = createUniqueId()): Promise<IRepoInfo> => {
        const storedInfo = this.getStoredInfo();
        if (storedInfo) {
            return storedInfo;
        }

        window.parent.postMessage(
            {
                type: 'vso-retrieve-repository-info',
                id,
            },
            document.referrer
        );

        const timeout = 5000;
        const data = await this.awaitOnResponse(id, timeout);

        if (data === null) {
            throw new Error(`Parent didn\'t respond on postMessage request in ${timeout}ms.`);
        }
        if (!this.isValidInfo(data)) {
            throw new Error(
                'No "repoName" or "repoOwnerUsername" is not set on the message from parent.'
            );
        }

        localStorage.setItem(this.localStorageKey, JSON.stringify(data, null, 2));

        return data;
    };

    private awaitOnResponse = async (id: string, timeoutMs: number): Promise<IRepoInfo | null> => {
        return new Promise((res, rej) => {
            this.awaitResponsePromises.set(id, [res, rej]);
            setTimeout(() => {
                res(null);
            }, timeoutMs);
        });
    };

    private receiveMessage = async (e: MessageEvent) => {
        // ignore non-github messages
        if (!isGithubTLD(e.origin)) {
            return;
        }

        const { responseId, type } = e.data;

        if (type !== 'vso-retrieve-repository-info-response') {
            return;
        }

        if (!responseId) {
            throw new Error(`Received a message from parent but "responseId" is not set.`);
        }

        const promiseFunctions = this.awaitResponsePromises.get(responseId);

        if (!promiseFunctions) {
            return;
        }

        const [resolve] = promiseFunctions;
        resolve(e.data);
    };
}
