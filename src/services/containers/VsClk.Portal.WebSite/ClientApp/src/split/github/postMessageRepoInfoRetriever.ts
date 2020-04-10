import { isGithubTLD } from 'vso-client-core';
import { createUniqueId } from '../../dependencies';
import { getCurrentEnvironmentId } from 'vso-workbench/src/utils/getCurrentEnvironmentId';

export interface IRepoInfo {
    ownerUsername: string;
    workspaceId: string;
    repositoryId: string;
    environmentId: string;
}

const getLocalstorageKey = () => {
    const envId = getCurrentEnvironmentId();

    return `vso-github-repo-info-${envId}`;
}

const isValidInfo = (info: IRepoInfo) => {
    return info.ownerUsername && info.workspaceId && info.repositoryId;
};

export class PostMessageRepoInfoRetriever {
    private awaitResponsePromises: Map<string, [Function, Function]> = new Map();

    constructor() {
        const envId = location.pathname.split('/')[2];
        if (!envId) {
            throw new Error('No environmentId found.'); 
        }

        window.addEventListener('message', this.receiveMessage, false);
    }

    public static getStoredInfo() {
        const storedInfoString = localStorage.getItem(getLocalstorageKey());
        if (storedInfoString) {
            try {
                const storedInfo = JSON.parse(storedInfoString) as IRepoInfo;

                if (isValidInfo(storedInfo)) {
                    return storedInfo;
                }
            } catch {}
        }
    }

    public getRepoInfo = async (id = createUniqueId()): Promise<IRepoInfo> => {
        const storedInfo = PostMessageRepoInfoRetriever.getStoredInfo();
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
        if (!isValidInfo(data)) {
            throw new Error(
                'No "repoName" or "repoOwnerUsername" is not set on the message from parent.'
            );
        }

        localStorage.setItem(getLocalstorageKey(), JSON.stringify(data, null, 2));

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

    public dispose() {
        window.removeEventListener('message', this.receiveMessage, false);
        this.awaitResponsePromises = new Map();
    }
}
