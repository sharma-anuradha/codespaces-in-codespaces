import React, { useEffect } from 'react';
import { TitleBar } from '../titlebar/titlebar';

import './gitHubLogin.css';
import { storeGitHubAccessTokenResponse } from '../../ts-agent/services/gitCredentialService';

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
        <div className='ms-Grid main'>
            <div className='ms-Grid-row'>
                <TitleBar />
            </div>
            <div className='ms-Grid-row main__app-content'>
                <div className='ms-Grid-col main__app-content-container'>
                    <div className='github-login'>{content}</div>
                </div>
            </div>
        </div>
    );
}
