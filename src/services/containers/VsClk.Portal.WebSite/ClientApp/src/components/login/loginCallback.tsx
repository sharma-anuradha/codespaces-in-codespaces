import React, { Component } from 'react';

import { loginPath, environmentsPath } from '../../routerPaths';
import { loginSilent, acquireTokenRedirect } from '../../actions/login';
import { Loader } from '../loader/loader';
import { Redirect } from 'react-router-dom';

let loginSilentPromise: any = undefined;

export interface LoginCallbackProps {}

interface LoginCallbackState {
    loginPending: boolean;
    loginPassed: boolean;
    loginFailed: boolean;
}

export class LoginCallback extends Component<LoginCallbackProps, LoginCallbackState> {
    constructor(props: LoginCallbackProps) {
        super(props);
        this.state = { loginPending: true, loginPassed: false, loginFailed: false };

        if (!loginSilentPromise) {
            loginSilentPromise = loginSilent().then(
                () => {
                    this.setState({ loginPending: false, loginPassed: true, loginFailed: false });
                },
                (err) => {
                    try {
                        if (err && err.name === 'InteractionRequiredAuthError') {
                            acquireTokenRedirect();
                        } else {
                            this.setState({
                                loginPending: false,
                                loginPassed: false,
                                loginFailed: true,
                            });
                        }
                    } catch (err) {
                        this.setState({
                            loginPending: false,
                            loginPassed: false,
                            loginFailed: true,
                        });
                    }
                }
            );
        }
    }

    render() {
        if (this.state.loginPassed) {
            return <Redirect to={environmentsPath} />;
        } else if (this.state.loginFailed) {
            return <Redirect to={loginPath} />;
        }
        return <Loader message='Signing in...' />;
    }
}
