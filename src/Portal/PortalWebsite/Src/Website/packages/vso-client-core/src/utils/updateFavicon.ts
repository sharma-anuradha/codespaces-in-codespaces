export const updateFavicon = (faviconPath: string) => {
    const links = document.querySelectorAll("#js-favicon, .js-favicon");
    if (!links) {
        return;
    }
    links.forEach(function (link) {
        link.setAttribute('href', faviconPath);
    });
};
