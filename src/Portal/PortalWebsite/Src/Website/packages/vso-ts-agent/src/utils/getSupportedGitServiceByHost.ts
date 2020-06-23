import { SupportedGitService } from '../interfaces/SupportedGitService';

export function getSupportedGitServiceByHost(host: string | undefined): SupportedGitService {
    if (!host) {
        return SupportedGitService.Unknown;
    }

    if (host.startsWith('www.')) {
        host = host.substr('www.'.length);
    }

    if (host.endsWith(SupportedGitService.AzureDevOps) ||
        (host.endsWith('.visualstudio.com') && host !== 'online.visualstudio.com')) {
        return SupportedGitService.AzureDevOps;
    }

    switch (host) {
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
