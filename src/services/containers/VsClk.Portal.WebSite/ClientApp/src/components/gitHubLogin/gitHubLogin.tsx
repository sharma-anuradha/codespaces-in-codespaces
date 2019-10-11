import React, { useEffect } from 'react';

import { PortalLayout } from '../portalLayout/portalLayout';
import { storeGitHubAccessTokenResponse } from '../../services/gitHubAuthenticationService';

import './gitHubLogin.css';

export function GitHubLogin() {
    const url = new URL(window.location.href);

    useEffect(() => {
        const accessToken = url.searchParams.get('accessToken');
        const state = url.searchParams.get('state');
        if (accessToken && state) {
            storeGitHubAccessTokenResponse({
                accessToken,
                state,
            });
        }
    });

    let content = (
        <div className='github-login__message'>
            You are signed in into GitHub now and can close this page.
        </div>
    );

    const errorMessageText = url.searchParams.get('errorMessage');
    if (errorMessageText) {
        content = <div className='github-login__message'>GitHub authentication failed.</div>;
    }

    return (
        <PortalLayout hideNavigation>
            <div className='github-login'>{content}</div>
        </PortalLayout>
    );
}
