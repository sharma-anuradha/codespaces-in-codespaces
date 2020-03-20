import { isGithubTLD } from 'vso-client-core';

export const isHostedOnGithub = () => {
    const { ancestorOrigins } = window.document.location;

    if (!ancestorOrigins || !ancestorOrigins[0]) {
        return false;
    }

    const parentOrigin = ancestorOrigins[0];
    return isGithubTLD(parentOrigin);
};
