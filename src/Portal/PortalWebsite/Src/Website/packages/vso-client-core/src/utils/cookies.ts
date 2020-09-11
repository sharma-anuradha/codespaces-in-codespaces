export const getCookies = (): Record<string, string> => {
    // tslint:disable-next-line: no-cookies
    const cookieStrings = document.cookie.split(';');
    const cookies: Record<string, string> = {};

    for (let cookieString of cookieStrings) {
        const pair = cookieString.split('=');
        const name = (pair[0] + '').trim();
        const value = unescape(pair.slice(1).join('='));

        cookies[name] = value;
    }

    return cookies;
};

export const getCookie = (cookieName: string): string | undefined => {
    const cookies = getCookies();

    return cookies[cookieName];
};

export const setCookie = (cookieName: string, cookieValue: string, expiresIn: number) => {
    const expiresDate = new Date();
    expiresDate.setTime(Date.now() + expiresIn);

    document.cookie = `${cookieName}=${cookieValue};${expiresDate.toUTCString()};path=/`;
};

export const cookies = {
    getCookies,
    getCookie,
    setCookie,
};
