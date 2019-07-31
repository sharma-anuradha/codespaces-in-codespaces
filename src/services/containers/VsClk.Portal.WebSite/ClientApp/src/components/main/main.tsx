import React, { Component, Fragment } from 'react';
import { Redirect, RouteComponentProps } from 'react-router';

import './main.css';

import { TitleBar } from '../titlebar/titlebar';
import { Loader } from '../loader/loader';

import { EnvironmentsPanel } from '../environmentsPanel/environments-panel';
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
            isAuthenticated: true,
        };
    }

    async componentWillMount() {
        const token = await authService.getCachedToken();

        this.setState({
            isAuthenticated: !!token,
        });
    }

    render() {
        const { loading, isAuthenticated } = this.state;

        if (!isAuthenticated) {
            return <Redirect to='/welcome' />;
        }

        if (loading) {
            return <Loader message='Loading...' />;
        }

        return (
            <Fragment>
                <TitleBar />
                <EnvironmentsPanel />
            </Fragment>
        );
    }
}
