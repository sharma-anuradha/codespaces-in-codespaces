const KNOWN_GITHUB_HOSTNAMES = [
    'local.github.dev',
    'dev.github.dev',
    'ppe.github.dev',
    'github.dev',
];

export const isGitHubHostname = (urlString: string) => {
    const url = new URL(urlString);
    const ghUrl = url
        .hostname
        .split('.')
        .splice(1)
        .join('.');

    return KNOWN_GITHUB_HOSTNAMES.includes(ghUrl);
};
