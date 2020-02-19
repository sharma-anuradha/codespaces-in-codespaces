import React, { useEffect } from 'react';

import { PortalLayout } from '../portalLayout/portalLayout';
import { storeGitHubAccessTokenResponse } from '../../services/gitHubAuthenticationService';

import './OAuthLogin.css';

export function GitHubLogin() {
    const url = new URL(window.location.href);

    useEffect(() => {
        const accessToken = url.searchParams.get('accessToken');
        const state = url.searchParams.get('state');
        const scope = url.searchParams.get('scope');
        if (accessToken && state && scope) {
            storeGitHubAccessTokenResponse({
                accessToken,
                state,
                scope,
            });
        }
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

    return (
        <PortalLayout hideNavigation>
            <div className='oauth-login'>{content}</div>
        </PortalLayout>
    );
}
