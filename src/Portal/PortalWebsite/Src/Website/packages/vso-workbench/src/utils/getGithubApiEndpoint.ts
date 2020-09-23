import { isGithubTLD, localStorageKeychain } from 'vso-client-core';

const defaultGitHubApiEndpoint = 'https://api.github.com';
const localApiEndpoint = 'https://api.github.localhost';

const GITHUB_API_ENDPOINT_KEY = 'codespaces.dev.github-api-keychain-key';

export const isValidGithubApiEndpoint = (endpoint: string) => {
    if (!endpoint) {
        return false;
    }

    try {
        const uri = new URL(endpoint);

        return isGithubTLD(uri.href);
    } catch {
        return false;
    }
};

const getGitHubApiEndpointInternal = async () => {
    if (!isGithubTLD(location.href)) {
        return defaultGitHubApiEndpoint;
    }

    const key = localStorage.getItem('vso-local-github-endpoint');
    if (key === 'true') {
        return localApiEndpoint;
    }

    const devPanelEndpoint = await localStorageKeychain.get(GITHUB_API_ENDPOINT_KEY);
    if (!devPanelEndpoint || !isValidGithubApiEndpoint(devPanelEndpoint)) {
        return defaultGitHubApiEndpoint;
    }

    return devPanelEndpoint;
};

export const getGitHubApiEndpoint = async (path?: string): Promise<string> => {
    const endpoint = await getGitHubApiEndpointInternal();

    if (!path) {
        return endpoint;
    }

    const url = new URL(path, endpoint);
    return url.toString();
};

export const setGitHubApiEndpoint = async (endpoint: string) => {
    if (!isValidGithubApiEndpoint(endpoint)) {
        throw new Error(`Invalid GitHub API endpoint "${endpoint}"`);
    }

    await localStorageKeychain.set(GITHUB_API_ENDPOINT_KEY, endpoint);
};
