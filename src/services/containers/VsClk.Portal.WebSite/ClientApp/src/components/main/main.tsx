import React, { Component } from 'react';
import { Redirect, RouteComponentProps } from 'react-router';

import './main.css';

import { TitleBar } from '../titlebar/titlebar';
import { Loader } from '../loader/loader';

import { EnvironmentsPanel } from '../environmentsPanel/environments-panel'
import { authService } from '../../services/authService';

interface MainProps extends RouteComponentProps {}

interface MainState {
    loading?: boolean;
    showNameModal?: boolean;
    isAuthenticated: boolean;
}

export class Main extends Component<MainProps, MainState> {

    constructor(props: any) {
        super(props);

        this.state = {
            loading: false,
            isAuthenticated: true
        };
    }

    async componentWillMount() {
        const token = await authService.getCachedToken();

        this.setState({
            isAuthenticated: !!token
        });
    }

    private renderIframe() {
        const hiddenIframeStyles = {
            position: 'absolute',
            width: '100%',
            height: '100%',
            opacity: 0,
            top: '-100%',
            left: '-100%',
            zIndex: -1
        } as any;

        const port = parseInt(localStorage.getItem('vsonline.port'), 10);
        const appUrl = `https://localhost:${port || '8000'}`;

        return (
            <iframe
                style={hiddenIframeStyles}
                src={appUrl}
            />
        );
    }

    render() {
        const { loading, isAuthenticated } = this.state;

        if (!isAuthenticated) {
            return (<Redirect to='/welcome' />);
        }

        if (loading) {
            return (<Loader message='Loading...'/>);
        }

        return (
            <div>
                <TitleBar />
                <EnvironmentsPanel />
                { this.renderIframe() }
            </div>
        );
    }
}