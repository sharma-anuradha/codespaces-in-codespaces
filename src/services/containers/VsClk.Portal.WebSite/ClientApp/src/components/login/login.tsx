import React, { Component } from 'react';
import { connect } from 'react-redux';
import { Redirect } from 'react-router-dom';
import { DefaultButton } from 'office-ui-fabric-react/lib/Button';
import { Label } from 'office-ui-fabric-react/lib/Label';

import { login } from '../../actions/login';

import './login.css';
import { ApplicationState } from '../../reducers/rootReducer';
import { Loader } from '../loader/loader';

interface LoginProps {
    isAuthenticated: boolean;
    isAuthenticating: boolean;
    login: (...name: Parameters<typeof login>) => void;
}

class LoginView extends Component<LoginProps> {
    render() {
        if (!this.props.isAuthenticated && this.props.isAuthenticating) {
            return <Loader message='Signing in...' />;
        }
        if (this.props.isAuthenticated) {
            return <Redirect to='/environments' />;
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
