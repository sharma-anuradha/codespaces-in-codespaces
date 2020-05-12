import { isGithubTLD } from './isGithubTLD';

export const isHostedOnGithub = () => {
    if (isGithubTLD(location.href)) {
        return true;
    }

    const { ancestorOrigins } = window.document.location;

    if (!ancestorOrigins || !ancestorOrigins[0]) {
        return false;
    }

    const parentOrigin = ancestorOrigins[0];
    return isGithubTLD(parentOrigin);
};
