import { isGithubTLD } from './isGithubTLD';
import { isSalesforceTLD } from './isSalesforceTLD';

export const isKnownPartnerTLD = (urlString: string): boolean => {
    return isGithubTLD(urlString)
        || isSalesforceTLD(urlString)
};
