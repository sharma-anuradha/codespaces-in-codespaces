import React, { Component } from 'react';
import './statusbar.css';

import { AuthService } from '../../services/authService';

export interface StatusBarProps {
}

export class StatusBar extends Component<StatusBarProps> {

    private loginForm: HTMLFormElement;

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
        const user = AuthService.Instance.getUser();

        return (
            <div className='part statusbar' style={{ backgroundColor: backgroundColor, position: 'absolute', color: 'rgb(255, 255, 255)', bottom: '0px' }}>
                {user ?
                    <div className='statusbar-item left statusbar-entry' statusbar-entry-priority='4' statusbar-entry-alignment='0'>
                        <div title={`Signed in to Visual Studio Web as ${user.name} <samelh@microsoft.com>`}>
                            <span className='octicon octicon-person '></span> {user.name}
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
                {user ?
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
