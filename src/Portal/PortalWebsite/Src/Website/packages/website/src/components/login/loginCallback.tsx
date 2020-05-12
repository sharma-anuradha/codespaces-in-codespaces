import React, { Component } from 'react';

import { loginPath, environmentsPath } from '../../routerPaths';
import { loginSilent, acquireTokenRedirect } from '../../actions/login';
import { Loader } from '../loader/loader';
import { Redirect } from 'react-router-dom';

let loginSilentPromise: any = undefined;

export interface LoginCallbackProps {}

enum LoginState {
    LoginPending = 'Pending',
    LoginPassed = 'Passed',
    LoginFailed = 'Failed',
}

interface LoginCallbackState {
    loginState: LoginState;
}

export class LoginCallback extends Component<LoginCallbackProps, LoginCallbackState> {
    constructor(props: LoginCallbackProps) {
        super(props);
        this.state = { loginState: LoginState.LoginPending };

        if (!loginSilentPromise) {
            loginSilentPromise = loginSilent().then(
                () => {
                    this.setState({ loginState: LoginState.LoginPassed });
                },
                (err) => {
                    try {
                        if (err && err.name === 'InteractionRequiredAuthError') {
                            acquireTokenRedirect();
                        } else {
                            this.setState({ loginState: LoginState.LoginFailed });
                        }
                    } catch (err) {
                        this.setState({ loginState: LoginState.LoginFailed });
                    }
                }
            );
        }
    }

    render() {
        if (this.state.loginState === LoginState.LoginPassed) {
            return <Redirect to={environmentsPath} />;
        } else if (this.state.loginState === LoginState.LoginFailed) {
            return <Redirect to={loginPath} />;
        }
        return <Loader message='Signing in...' />;
    }
}
