import { GITHUB_DOT_DEV_TLD, GITHUB_LOCAL_TLD } from '../constants';
import { getParentDomain } from './getParentDomain';

const GITHUB_TLDS = [
    'github.com',
    GITHUB_DOT_DEV_TLD,
    GITHUB_LOCAL_TLD,
];

export const isGithubTLD = (urlString: string): boolean => {
    const tld = getParentDomain(urlString);
    return GITHUB_TLDS.includes(tld);
};

export const isGithubDotDevTLD = (urlString: string): boolean => {
    return GITHUB_DOT_DEV_TLD === getParentDomain(urlString);
};

export const isGithubLocalTLD = (urlString: string): boolean => {
    return GITHUB_LOCAL_TLD === getParentDomain(urlString);
};
