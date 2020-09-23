
export const appendUrlPath = (urlString: string, pathString: string): string => {
    const url = new URL(urlString);

    const newPath = `${url.pathname}` + `${pathString}`;
    url.pathname = newPath.replace(/\/\/*/, '/');

    return url.toString();
};
