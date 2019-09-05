import React, { Component } from 'react';
import { connect } from 'react-redux';
import { Redirect } from 'react-router-dom';
import { DefaultButton } from 'office-ui-fabric-react/lib/Button';
import { Label } from 'office-ui-fabric-react/lib/Label';

import { signIn } from '../../actions/signIn';

import './welcome.css';
import { ApplicationState } from '../../reducers/rootReducer';
import { Loader } from '../loader/loader';

interface WelcomeProps {
    isAuthenticated: boolean;
    isAuthenticating: boolean;
    signIn: (...name: Parameters<typeof signIn>) => void;
}

class WelcomeView extends Component<WelcomeProps> {
    render() {
        if (!this.props.isAuthenticated && this.props.isAuthenticating) {
            return <Loader message='Signing in...' />;
        }
        if (this.props.isAuthenticated) {
            return <Redirect to='/environments' />;
        }

        return (
            <div className='welcome-page'>
                <div className='welcome-page__sign-in-buttons'>
                    <Label className='welcome-page__sign-in-label'>Something exciting</Label>
                    <DefaultButton
                        className='welcome-page__sign-in-button'
                        text='Sign up'
                        primary={true}
                        onClick={this.props.signIn}
                    />
                    <DefaultButton
                        className='welcome-page__sign-in-button'
                        text='Sign in'
                        onClick={this.props.signIn}
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
    signIn,
};

export const WelcomeConnected = connect(
    getAuthState,
    actions
)(WelcomeView);

// Router cannot consume connected components properly so we wrap welcome

export function Welcome() {
    return <WelcomeConnected />;
}
