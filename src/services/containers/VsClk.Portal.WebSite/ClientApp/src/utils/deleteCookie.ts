export const deleteCookie = (cookieName: string) => {
    // tslint:disable-next-line: no-cookies
    document.cookie = `${cookieName}=; Max-Age=0`;
};
