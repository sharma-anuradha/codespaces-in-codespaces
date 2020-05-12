
export const isLocalhostTLD = (urlString: string): boolean => {
    const url = new URL(urlString);
    const locationSplit = url.hostname.split('.');
    const mainDomain = locationSplit[locationSplit.length - 1];

    return mainDomain === 'localhost';
};


