import React, { Component } from 'react';
import './statusbar.css';

import { authService } from '../../services/authService';

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

        this.state = {};
    }

    async componentWillMount() {
        const token = await authService.getCachedToken();

        if (!token) {
            return token;
        }

        const { account } = token;

        this.setState({
            username: account.name,
            useremail: account.userName
        });
    }

    handleLoginClick = () => {
        this.loginForm.submit();
    }

    handleLogoutClick = () => {
        authService.signOut().then(() => {
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
            <div />
        );
    }
}
