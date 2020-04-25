import {
    localStorageKeychain,
    getCurrentEnvironmentId,
} from 'vso-client-core';

import { createGitHubTokenKey } from './createGitHubTokenKey';

export const getGithubToken = async (): Promise<string | null> => {
    const githubKey = createGitHubTokenKey(getCurrentEnvironmentId());
    const token = (await localStorageKeychain.get(githubKey));

    return token || null;
};
