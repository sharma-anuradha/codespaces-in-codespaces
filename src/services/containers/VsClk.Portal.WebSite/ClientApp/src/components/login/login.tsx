import React, { useEffect, useState } from 'react';
import { connect } from 'react-redux';
import { Redirect } from 'react-router-dom';
import { DefaultButton } from 'office-ui-fabric-react/lib/Button';
import { Label } from 'office-ui-fabric-react/lib/Label';

import { login } from '../../actions/login';

import './login.css';
import { ApplicationState } from '../../reducers/rootReducer';
import { Loader } from '../loader/loader';
import { environmentsPath } from '../../routes';
import { ITokenWithMsalAccount } from '../../typings/ITokenWithMsalAccount';
import { setAuthCookie } from '../../utils/setAuthCookie';

interface LoginProps {
    redirectUrl: string | null;
    token: ITokenWithMsalAccount | undefined;
    isAuthenticated: boolean;
    isAuthenticating: boolean;
    login: (...name: Parameters<typeof login>) => void;
}

function withAllowedSubdomain(targetUrl: URL) {
    const [sessionAndPort, app, ...rest] = targetUrl.hostname.split('.');
    if (app === 'app' && rest.join('.') === location.hostname) {
        targetUrl.hostname = [sessionAndPort, app, location.hostname].join('.');

        return targetUrl.toString();
    }
    return new URL(environmentsPath, location.origin).toString();
}

function LoginView(props: LoginProps) {
    const [isAuthCookieSet, setIsAuthCookieSet] = useState(false);

    useEffect(() => {
        if (!props.token) {
            return;
        }

        setAuthCookie(props.token.accessToken).then(
            () => {
                setIsAuthCookieSet(true);
            },
            () => {
                // noop
            }
        );
    }, [setIsAuthCookieSet, props.token]);

    if (!props.isAuthenticated && props.isAuthenticating) {
        return <Loader message='Signing in...' />;
    }
    if (props.isAuthenticated) {
        if (!props.redirectUrl) {
            return <Redirect to={environmentsPath} />;
        }

        const redirectUrl = new URL(props.redirectUrl, location.origin);
        if (redirectUrl.origin === location.origin) {
            return <Redirect to={redirectUrl.toString().substr(redirectUrl.origin.length)} />;
        }

        if (isAuthCookieSet) {
            window.location.href = withAllowedSubdomain(redirectUrl);
        } else {
            return <Loader message='Signing in...' />;
        }
    }

    return (
        <div className='login-page'>
            <div className='login-page__sign-in-buttons'>
                <Label className='login-page__sign-in-label'>Something exciting</Label>
                <DefaultButton
                    className='login-page__sign-in-button'
                    text='Sign in'
                    primary={true}
                    onClick={props.login}
                />
            </div>
        </div>
    );
}

const getAuthState = (state: ApplicationState) => ({
    redirectUrl: new URLSearchParams(location.search).get('redirectUrl'),
    token: state.authentication.token,
    isAuthenticated: state.authentication.isAuthenticated,
    isAuthenticating: state.authentication.isAuthenticating,
});
const actions = {
    login,
};

export const LoginConnected = connect(
    getAuthState,
    actions
)(LoginView);

export function Login() {
    return <LoginConnected />;
}
