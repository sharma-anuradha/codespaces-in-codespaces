import { AuthenticationParameters } from 'msal';

import { authService } from '../authService';

import { ServerRequestParameters } from './serverRequestParameters';
import { AuthorityFactory } from './authorityFactory';
import { UrlUtils } from './urlUtils';

const VSCODE_APP_ID = 'aebc6443-996d-45c2-90f0-388ff96faa56';
const ARM_RESOURCE_ID = 'https://management.core.windows.net';

export const createNavigateUrl = async (tenantId: string, nonce: string) => {
    const currentToken = await authService.getCachedToken();

    if (!currentToken) {
        throw new Error('User is not authenticated.')
    }

    const aadAuthorityUrlOrganizations = `https://login.microsoftonline.com/${tenantId}`;
    const tokenRequest: AuthenticationParameters = {
        scopes: [`${ARM_RESOURCE_ID}/.default`],
        authority: aadAuthorityUrlOrganizations,
        state: nonce
    };

    const authority = AuthorityFactory.CreateInstance(aadAuthorityUrlOrganizations, false);

    if (!authority) {
        throw new Error('No authority created.');
    }

    const serverAuthenticationRequest = new ServerRequestParameters(
        authority,
        VSCODE_APP_ID,
        tokenRequest.scopes!,
        'token',
        location.origin,
        tokenRequest.state!
    );

    if (currentToken.account) {
        serverAuthenticationRequest.populateQueryParams(currentToken.account, tokenRequest);
    }

    await serverAuthenticationRequest.authorityInstance.resolveEndpointsAsync();

    const urlString = UrlUtils.createNavigateUrl(serverAuthenticationRequest);
    const url = new URL(urlString);

    url.searchParams.set('prompt', 'none');

    return url;
}
