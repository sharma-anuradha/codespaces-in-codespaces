export const isGithubTLD = (urlString: string) => {
    const url = new URL(urlString);

    const locationSplit = url.hostname.split('.');
    const mainDomain = locationSplit
        .slice(locationSplit.length - 2)
        .join('.');

    return (mainDomain === 'github.com') || (mainDomain === 'github.localhost');
}

export const isHostedOnGithub = () => {
    const { ancestorOrigins } = window.document.location;

    if (!ancestorOrigins || !ancestorOrigins[0]) {
        return false;
    }

    const parentOrigin = ancestorOrigins[0];
    return isGithubTLD(parentOrigin);
};
