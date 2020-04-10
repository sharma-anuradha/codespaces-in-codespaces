import { isHostedOnGithub } from 'vso-client-core';

import { CrossDomainPFAuthenticator } from 'vso-workbench';

import { getCurrentEnvironment, getPFDomain } from './getPortForwardingDomain';

export async function setAuthCookie(accessToken: string, endpoint: string) {
    const crossDomainAuth = new CrossDomainPFAuthenticator(endpoint);
    const tokenName = isHostedOnGithub() ? 'cascadeToken' : 'token';

    await crossDomainAuth.setPFCookie(accessToken, tokenName);
    crossDomainAuth.dispose();
}

export async function deleteAuthCookie() {
    const environment = getCurrentEnvironment();
    const pfDomain = getPFDomain(environment);

    const crossDomainAuth = new CrossDomainPFAuthenticator(pfDomain);
    await crossDomainAuth.removePFCookieWithCascadeToken();
    crossDomainAuth.dispose();
}
