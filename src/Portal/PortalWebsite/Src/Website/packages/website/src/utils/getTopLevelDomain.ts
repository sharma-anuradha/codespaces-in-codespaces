
export const getTopLevelDomain = (urlString: string) => {
    const url = new URL(urlString);
    const reverseSplit = url.hostname.split('.').reverse();

    return reverseSplit.slice(0, 2).reverse().join('.');
}
