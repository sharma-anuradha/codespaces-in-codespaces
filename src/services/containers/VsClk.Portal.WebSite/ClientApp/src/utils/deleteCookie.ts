
export const deleteCookie = (cookieName: string) => {
    document.cookie = `${cookieName}=; Max-Age=0`;
};
