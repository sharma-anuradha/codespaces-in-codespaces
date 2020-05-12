import { isHostedOnGithub } from 'vso-client-core';

import { CrossDomainPFAuthenticator } from 'vso-workbench';

export async function setAuthCookie(accessToken: string, endpoint: string) {
    const crossDomainAuth = new CrossDomainPFAuthenticator(endpoint);
    const tokenName = isHostedOnGithub() ? 'cascadeToken' : 'token';

    await crossDomainAuth.setPFCookie(accessToken, tokenName);
    crossDomainAuth.dispose();
}
