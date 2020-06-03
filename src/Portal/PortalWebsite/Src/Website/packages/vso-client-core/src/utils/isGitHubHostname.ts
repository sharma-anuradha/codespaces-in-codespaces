const KNOWN_GITHUB_HOSTNAMES = [
    'local.github.dev',
    'dev.github.dev',
    'ppe.github.dev',
    'github.dev',

    'workspaces-dev.github.com',
    'workspaces-ppe.github.com',
    'workspaces.github.com',

    'codespaces-dev.github.com',
    'codespaces-ppe.github.com',
    'codespaces.github.com'
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
