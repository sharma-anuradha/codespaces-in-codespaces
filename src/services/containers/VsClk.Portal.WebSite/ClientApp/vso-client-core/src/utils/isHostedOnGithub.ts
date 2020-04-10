import { isGithubTLD } from './isGithubTLD';


export const isHostedOnGithub = () => {
    const { ancestorOrigins } = window.document.location;

    if (!ancestorOrigins || !ancestorOrigins[0]) {
        return false;
    }

    const parentOrigin = ancestorOrigins[0];
    return isGithubTLD(parentOrigin);
};
