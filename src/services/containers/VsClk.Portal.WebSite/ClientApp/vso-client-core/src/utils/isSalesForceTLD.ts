export const isSalesForceTLD = (urlString: string): boolean => {
    const url = new URL(urlString);
    const locationSplit = url.hostname.split('.');
    const mainDomain = locationSplit.slice(locationSplit.length - 2).join('.');
    return mainDomain === 'salesforce.com';
};
