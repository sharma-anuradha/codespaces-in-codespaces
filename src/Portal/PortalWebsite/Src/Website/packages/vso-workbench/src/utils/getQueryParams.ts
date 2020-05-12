export const getQueryParams = (url: string) => {
    const queryString = url.split('?')[1];
    if (queryString) {
        return new URLSearchParams(`?${queryString}`);
    }
    return new URLSearchParams();
};
