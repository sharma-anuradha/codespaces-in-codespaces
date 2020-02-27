import { evaluateFeatureFlag, azureDevOpsOAuth } from "./featureSet";

export enum SupportedGitService {
    Unknown,
    GitHub = 'github.com',
    BitBucket = 'bitbucket.org',
    GitLab = 'gitlab.com',
    AzureDevOps = 'dev.azure.com',
}

export function getSupportedGitService(url: string): SupportedGitService {
    const parsedUrl = new URL(url);
    if (parsedUrl.host.startsWith('www.')) {
        parsedUrl.host = parsedUrl.host.substr('www.'.length);
    }

    if (parsedUrl.host.endsWith(SupportedGitService.AzureDevOps) || parsedUrl.host.endsWith(".visualstudio.com")) {
        return SupportedGitService.AzureDevOps;
    }

    switch (parsedUrl.host) {
        case SupportedGitService.GitHub:
            return SupportedGitService.GitHub;

        case SupportedGitService.BitBucket:
            return SupportedGitService.BitBucket;

        case SupportedGitService.GitLab:
            return SupportedGitService.GitLab;

        default:
            return SupportedGitService.Unknown;
    }
}

function isGitHubRepositoryName(repositoryName: string) {
    // GitHub allows organization names to contain alphanumeric characters + hyphens. They cannot start or end in hyphen.
    // For repository names they turn all non-hyphen symbols to hyphens and allow underscores, dots, and hyphens in the beginning and end.
    const shortGitHubUrlRegex = /^([a-zA-Z0-9][a-zA-Z0-9-]+[a-zA-Z0-9]|[a-zA-Z0-9]+)\/[_\-a-zA-Z0-9\.]+$/;

    return shortGitHubUrlRegex.test(repositoryName);
}

export function isRecognizedGitUrl(maybeUrl: string): boolean {
    maybeUrl = maybeUrl.trim();
    if (!maybeUrl) {
        return false;
    }

    if (isGitHubRepositoryName(maybeUrl)) {
        return true;
    }

    try {
        const parsedUrl = new URL(maybeUrl);

        // tslint:disable-next-line: no-http-string
        if (parsedUrl.protocol !== 'http:' && parsedUrl.protocol !== 'https:') {
            return false;
        }

        const service = getSupportedGitService(maybeUrl);
        if (service === SupportedGitService.Unknown || (service === SupportedGitService.AzureDevOps && !evaluateFeatureFlag(azureDevOpsOAuth))) {
            return false;
        }
        if (service === SupportedGitService.AzureDevOps) {
            return true;
        }

        if (!isMinimalSupportedPath(parsedUrl)) {
            return false;
        }

        if (!isSupportedGithubPath(parsedUrl)) {
            return false;
        }

        return true;
    } catch {
        return false;
    }

    function isMinimalSupportedPath(url: URL): boolean {
        // Supported services need at least 2 path parts to identify <org/repo> pair
        return url.pathname.substr(1).split('/').length >= 2;
    }

    function isSupportedGithubPath(url: URL) {
        // We can get rid of the leading / from pathname for this analysis
        let path = url.pathname.substr(1);

        // We can get rid of the trailing / from pathname because git clone can handle that.
        if (path.endsWith('/')) {
            path = path.substr(0, path.length - 1);
        }

        // We can get rid of the trailing .git from pathname because git clone can handle that.
        if (path.endsWith('.git')) {
            path = path.substr(0, path.length - '.git'.length);
        }

        if (isGitHubRepositoryName(path)) {
            return true;
        }

        // Commit urls are in the form of:
        //      https://github.com/owner/repository/commit/AAAAAAAAAAAAAAAAAA
        // Tree urls are branches in the form of:
        //      https://github.com/owner/repository/tree/name/of/branch
        // Pull requests are in the form of:
        //      https://github.com/vsls-contrib/test/pull/18
        const [org, repository, type, ...rest] = path.split('/');
        if (type === 'commit' || type === 'tree')
        {
            if (
                isGitHubRepositoryName(`${org}/${repository}`) &&
                rest.length >= 1
            ) {
                return true;
            }
        }

        if (type === 'pull' && rest.length === 1) {
            const pullRequestNumber = Number.parseInt(rest[0], 10);
            if (
                isGitHubRepositoryName(`${org}/${repository}`) &&
                pullRequestNumber &&
                pullRequestNumber > 0
            ) {
                return true;
            }
        }

        return false;
    }
}

export function normalizeGitUrl(maybeUrl: string): string | undefined {
    if (!isRecognizedGitUrl(maybeUrl)) {
        return undefined;
    }

    if (isGitHubRepositoryName(maybeUrl)) {
        // Transform repository name (vso/test) into URL provided in GitHub
        // clone dialog, also make sure everything is ok URL-wise.
        const url = new URL(`${maybeUrl}.git`, 'https://github.com');
        return url.toString();
    }

    return maybeUrl;
}

export function getQueryableUrl(maybeUrl: string): string | undefined {
    if (!isRecognizedGitUrl(maybeUrl)) {
        return undefined;
    }

    const normalizedUrl = normalizeGitUrl(maybeUrl);
    if (!normalizedUrl) {
        return undefined;
    }

    const url = new URL(normalizedUrl);
    const service = getSupportedGitService(normalizedUrl);

    switch (service) {
        case SupportedGitService.GitHub: {
            const repositoryName = getRepositoryName(url);
            const queryableUrl = new URL(`/repos/${repositoryName}`, 'https://api.github.com');
            return queryableUrl.toString();
        }

        // BitBucket API docs
        //
        //      https://developer.atlassian.com/bitbucket/api/2/reference/resource/repositories/%7Busername%7D/%7Brepo_slug%7D#get
        //
        case SupportedGitService.BitBucket: {
            const repositoryName = getRepositoryName(url);
            const queryableUrl = new URL(
                `/2.0/repositories/${repositoryName}`,
                'https://api.bitbucket.org'
            );
            return queryableUrl.toString();
        }

        // GitLab API docs
        //
        //      https://docs.gitlab.com/ee/api/projects.html#get-single-project
        //
        case SupportedGitService.GitLab: {
            const repositoryName = getRepositoryName(url);
            const queryableUrl = new URL(
                // GitLab expects the repository name to be url encoded (or it's ID)
                `/api/v4/projects/${encodeURIComponent(repositoryName)}`,
                'https://gitlab.com'
            );
            return queryableUrl.toString();
        }

        // Azure DevOps API docs
        //
        //      https://docs.microsoft.com/en-us/rest/api/azure/devops/git/repositories/list?view=azure-devops-rest-5.1
        //
        case SupportedGitService.AzureDevOps: {
            const repositoryName = getRepositoryName(url);
            const queryableUrl = new URL(
                `/${encodeURIComponent(repositoryName)}/_apis/git/repositories?api-version=5.1`,
                'https://dev.azure.com'
            );
            return queryableUrl.toString();
        }

        default:
            return undefined;
    }

    function getRepositoryName(url: URL) {
        // Org and repository will be valid at this point as it passed validation
        let [org, repository] = url.pathname.substr(1).split('/');
        // repository might still have trailing ".git" attached though.
        if (repository.endsWith('.git')) {
            repository = repository.substr(0, repository.length - '.git'.length);
        }

        return `${org}/${repository}`;
    }
}
