const GITHUB_TLDS = [
    'github.com',
    'github.dev',
    'github.localhost',
];

export const isGithubTLD = (urlString: string): boolean => {
    const url = new URL(urlString);
    const locationSplit = url.hostname.split('.');
    const mainDomain = locationSplit.slice(locationSplit.length - 2).join('.');
    return GITHUB_TLDS.includes(mainDomain);
};
