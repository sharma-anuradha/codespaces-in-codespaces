export const getQueryParams = (url: string): URLSearchParams => {
    const queryString = url.split('?')[1];
    const params = new URLSearchParams(`?${queryString}`);

    return params;
}
