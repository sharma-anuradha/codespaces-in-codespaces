import React, { Component } from 'react';
import { Redirect } from 'react-router';
import { Button } from 'office-ui-fabric-react/lib/Button';
import { Label } from 'office-ui-fabric-react/lib/Label';

import { authService } from '../../services/authService';

import './welcome.css';

interface WelcomeProps {}

interface WelcomeState {
    isAuthenticated: boolean;
}

export class Welcome extends Component<WelcomeProps, WelcomeState> {
    constructor(props: WelcomeProps) {
        super(props);

        this.state = {
            isAuthenticated: false,
        };
    }
    private onSignIn = async () => {
        const token = await authService.signIn();

        if (token) {
            this.setState({ isAuthenticated: true });
        }
    };

    async componentWillMount() {
        const token = await authService.getCachedToken();

        this.setState({
            isAuthenticated: !!token,
        });
    }

    render() {
        if (this.state.isAuthenticated) {
            return <Redirect to='/environments' />;
        }

        return (
            <div className='welcome-page'>
                <div className='welcome-page__sign-in-buttons'>
                    <Label className='welcome-page__sign-in-label'>Something exciting</Label>
                    <Button
                        className='welcome-page__sign-in-button'
                        text='Sign up'
                        primary={true}
                        onClick={this.onSignIn}
                    />
                    <Button
                        className='welcome-page__sign-in-button'
                        text='Sign in'
                        onClick={this.onSignIn}
                    />
                </div>
            </div>
        );
    }
}
