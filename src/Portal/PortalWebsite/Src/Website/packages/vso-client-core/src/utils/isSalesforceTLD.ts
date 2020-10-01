import { getParentDomain } from './getParentDomain';

const SALESFORCE_TLDS = [
    'builder.code.com',
];

export const isSalesforceTLD = (urlString: string): boolean => {
    const tld = getParentDomain(urlString, 3);

    return SALESFORCE_TLDS.includes(tld);
};
