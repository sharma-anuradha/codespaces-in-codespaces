import { isSalesForceTLD } from './isSalesForceTLD';
import { isGithubTLD } from './isGithubTLD';
import { isLocalhostTLD } from './isLocalhostTLD';

export const isKnownPartnerTLD = (urlString: string): boolean => {
    return isGithubTLD(urlString)
        || isSalesForceTLD(urlString)
        || isLocalhostTLD(urlString);
};
