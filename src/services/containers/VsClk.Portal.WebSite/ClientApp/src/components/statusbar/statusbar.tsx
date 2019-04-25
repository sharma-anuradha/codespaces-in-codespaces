import React, { Component } from 'react';
import './statusbar.css';

import { AuthService } from '../../services/authService';

export interface StatusBarProps {
}

export interface StatusBarState {
    username?: string;
    useremail?: string;
}

export class StatusBar extends Component<StatusBarProps, StatusBarState> {

    private loginForm: HTMLFormElement;

    constructor(props: StatusBarProps) {
        super(props);

        this.state = {
        }
    }

    async componentWillMount() {
        const user = await AuthService.Instance.getUser();
        this.setState({
            username: user ? user.name : undefined,
            useremail: user ? user.email : undefined
        })
    }

    handleLoginClick = () => {
        this.loginForm.submit();
    }

    handleLogoutClick = () => {
        AuthService.Instance.logout().then(() => {
            window.location.reload();
        });
    }

    handleLoginFormRef = (c: HTMLFormElement) => {
        this.loginForm = c;
    }

    render() {
        const backgroundColor = '#5D1F6F';
        const { username, useremail } = this.state;

        return (
            <div className='part statusbar' style={{ backgroundColor: backgroundColor, position: 'absolute', color: 'rgb(255, 255, 255)', bottom: '0px' }}>
                {username ?
                    <div className='statusbar-item left statusbar-entry' statusbar-entry-priority='4' statusbar-entry-alignment='0'>
                        <div title={`Signed in to Visual Studio Online as ${username} <${useremail}>`}>
                            <span className='octicon octicon-person '></span> {username}
                        </div>
                    </div>
                    :
                    <div role='button' className='statusbar-item left statusbar-entry' statusbar-entry-priority='3' statusbar-entry-alignment='0' onClick={this.handleLoginClick}>
                        <form id='signin-form' ref={this.handleLoginFormRef} action="/signin" method="post">
                            <input type="hidden" name="Provider" value="Microsoft" />
                            <a type="submit" title='Login'>
                                <span className='octicon octicon-person '></span> Login
                            </a>
                        </form>
                    </div>}
                {username ?
                    <div role='button' className="statusbar-item right statusbar-entry" statusbar-entry-priority="-100" statusbar-entry-alignment="1" onClick={this.handleLogoutClick}>
                        <a title='Login'>
                            <span className='octicon octicon-link-external '></span>
                        </a>
                    </div>
                    : undefined}
            </div>
        );
    }
}
