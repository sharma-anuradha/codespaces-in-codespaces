import { CrossDomainPFAuthenticator } from './CrossDomainPFAuthenticator';

export async function setAuthCookie(accessToken: string, endpoint: string) {
    const crossDomainAuth = new CrossDomainPFAuthenticator(endpoint);
    await crossDomainAuth.setPFCookie(accessToken, 'cascadeToken');

    crossDomainAuth.dispose();
}

export async function deleteAuthCookie(endpoint: string) {
    const crossDomainAuth = new CrossDomainPFAuthenticator(endpoint);

    await crossDomainAuth.removePFCookieWithCascadeToken();
    crossDomainAuth.dispose();
}
