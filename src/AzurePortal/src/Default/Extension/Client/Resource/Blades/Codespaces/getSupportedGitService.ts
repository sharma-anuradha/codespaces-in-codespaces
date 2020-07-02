export enum SupportedGitService {
    Unknown,
    GitHub = 'github.com',
    BitBucket = 'bitbucket.org',
    GitLab = 'gitlab.com',
    AzureDevOps = 'dev.azure.com',
}

export function getSupportedGitService(url: string): SupportedGitService {
    const parsedUrl = new URL(url);
    return getSupportedGitServiceByHost(parsedUrl.host);
}

export function getSupportedGitServiceByHost(host: string | undefined): SupportedGitService {
    if (!host) {
        return SupportedGitService.Unknown;
    }

    if (host.startsWith('www.')) {
        host = host.substr('www.'.length);
    }

    if (host.endsWith(SupportedGitService.AzureDevOps) ||
        (host.endsWith(".visualstudio.com") && host !== "online.visualstudio.com")) {
        return SupportedGitService.AzureDevOps;
    }

    switch (host) {
        case SupportedGitService.GitHub:
            return SupportedGitService.GitHub;

        default:
            return SupportedGitService.Unknown;
    }
}