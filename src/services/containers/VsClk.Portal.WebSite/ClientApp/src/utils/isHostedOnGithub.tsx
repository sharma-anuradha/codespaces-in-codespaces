export const isHostedOnGithub = () => {
    const locationSplit = location.hostname.split('.');
    const mainDomain = locationSplit
        .slice(locationSplit.length - 2)
        .join('.');
    return (mainDomain === 'github.com');
};

export const isHostedOnLocalGithubStamp = () => {
    const result = location.hostname === "local.code.github.com";

    return result;
};
