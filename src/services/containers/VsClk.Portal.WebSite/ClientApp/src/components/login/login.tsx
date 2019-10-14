import React, { Component } from 'react';
import { connect } from 'react-redux';
import { Redirect } from 'react-router-dom';
import { DefaultButton } from 'office-ui-fabric-react/lib/Button';
import { Label } from 'office-ui-fabric-react/lib/Label';

import { login } from '../../actions/login';

import './login.css';
import { ApplicationState } from '../../reducers/rootReducer';
import { Loader } from '../loader/loader';
import { environmentsPath } from '../../routes';

interface LoginProps {
    redirectUrl: string | null;
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

class LoginView extends Component<LoginProps> {
    render() {
        if (!this.props.isAuthenticated && this.props.isAuthenticating) {
            return <Loader message='Signing in...' />;
        }
        if (this.props.isAuthenticated) {
            if (!this.props.redirectUrl) {
                return <Redirect to={environmentsPath} />;
            }

            const redirectUrl = new URL(this.props.redirectUrl, location.origin);
            if (redirectUrl.origin === location.origin) {
                return <Redirect to={redirectUrl.toString().substr(redirectUrl.origin.length)} />;
            }

            window.location.href = withAllowedSubdomain(redirectUrl);
        }

        return (
            <div className='login-page'>
                <div className='login-page__sign-in-buttons'>
                    <Label className='login-page__sign-in-label'>Something exciting</Label>
                    <DefaultButton
                        className='login-page__sign-in-button'
                        text='Sign in'
                        primary={true}
                        onClick={this.props.login}
                    />
                </div>
            </div>
        );
    }
}

const getAuthState = (state: ApplicationState) => ({
    redirectUrl: new URLSearchParams(location.search).get('redirectUrl'),
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
