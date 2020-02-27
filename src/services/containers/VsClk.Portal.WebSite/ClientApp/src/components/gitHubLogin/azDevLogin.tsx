import React, { useEffect } from 'react';

import { PortalLayout } from '../portalLayout/portalLayout';
import { storeAzDevAccessTokenResponse } from '../../services/azDevAuthenticationService';

import './OAuthLogin.css';

export function AzDevLogin() {
    const url = new URL(window.location.href);

    useEffect(() => {
        const accessToken = url.searchParams.get('accessToken');
        const state = url.searchParams.get('state');
        const scope = url.searchParams.get('scope');
        const refreshToken = url.searchParams.get('refreshToken');
        const expiresInStr = url.searchParams.get('expiresIn');
        if (accessToken && state && scope && refreshToken && expiresInStr) {
            const expiresInInt = parseInt(expiresInStr);
            const expiresOn = new Date(new Date().getTime() + expiresInInt * 1000);
            storeAzDevAccessTokenResponse({
                accessToken,
                state,
                scope,
                refreshToken,
                expiresOn,
            });
        }
    });

    let content = (
        <div className='oauth-login__message'>
            You are signed in into Azure DevOps now and can close this page.
        </div>
    );

    const errorMessageText = url.searchParams.get('errorMessage');
    if (errorMessageText) {
        content = <div className='oauth-login__message'>Azure DevOps authentication failed.</div>;
    }

    return (
        <PortalLayout hideNavigation>
            <div className='oauth-login'>{content}</div>
        </PortalLayout>
    );
}
