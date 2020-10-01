
export const getParentDomain = (urlString: string, n = 2) => {
    const url = new URL(urlString);
    const reverseSplit = url.hostname.split('.').reverse();

    return reverseSplit.slice(0, n).reverse().join('.');
}
