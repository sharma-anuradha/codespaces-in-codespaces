const KNOWN_GITHUB_HOSTNAMES = ['workspaces-dev.github.com'];

export const isGitHubHostname = (urlString: string) => {
    const url = new URL(urlString);
    const ghUrl = url
        .hostname
        .split('.')
        .splice(1)
        .join('.');

    return KNOWN_GITHUB_HOSTNAMES.includes(ghUrl);
};
