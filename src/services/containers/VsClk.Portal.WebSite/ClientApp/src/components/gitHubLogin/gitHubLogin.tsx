import React, { useEffect } from 'react';

import { PortalLayout } from '../portalLayout/portalLayout';
import { storeGitHubAccessTokenResponse } from '../../services/gitHubAuthenticationService';

import './OAuthLogin.css';

const getRedirectionUrl = (state: string) => {
    const split = state.split(',');
    if (split.length < 2) {
        return null;
    }

    const redirect = split[1];
    if (!redirect) {
        return null;
    }

    const redirectUrl = decodeURIComponent(redirect);

    return new URL(redirectUrl, location.origin);
}

export function GitHubLogin() {
    const url = new URL(window.location.href);

    useEffect(() => {
        const handle = async () => {
            const accessToken = url.searchParams.get('accessToken');
            const state = url.searchParams.get('state');
            const scope = url.searchParams.get('scope');

            if (accessToken && state && scope) {
                await storeGitHubAccessTokenResponse({
                    accessToken,
                    state,
                    scope,
                });
                
                const redirectUrl = getRedirectionUrl(state);

                if (redirectUrl) {
                    // hard redirect on-purpose in case of inline auth flow,
                    // to reinitialize all modules after auth
                    location.href = redirectUrl.pathname;
                }
            }
        }

        handle();
    });

    let content = (
        <div className='oauth-login__message'>
            You are signed in into GitHub now and can close this page.
        </div>
    );

    const errorMessageText = url.searchParams.get('errorMessage');
    if (errorMessageText) {
        content = <div className='oauth-login__message'>GitHub authentication failed.</div>;
    }

    const state = url.searchParams.get('state');
    if (state && getRedirectionUrl(state)) {
        content = <div className='github-login__message'></div>;
    }

    return (
        <PortalLayout hideNavigation>
            <div className='oauth-login'>{content}</div>
        </PortalLayout>
    );
}
