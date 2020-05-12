export const updateFavicon = (faviconPath: string) => {
    const link = document.querySelector("#js-favicon");
    if (!link) {
        return;
    }
    link.setAttribute('href', faviconPath);
};
