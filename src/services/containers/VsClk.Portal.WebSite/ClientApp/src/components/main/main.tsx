import React, { Component, Fragment } from 'react';
import { Redirect, RouteComponentProps } from 'react-router';

import './main.css';

import { TitleBar } from '../titlebar/titlebar';
import { Loader } from '../loader/loader';

import { EnvironmentsPanel } from '../environmentsPanel/environments-panel';
import { authService } from '../../services/authService';
import { configAMD } from '../../amd/amdConfig';
import EnvRegService from '../../services/envRegService';

declare var AMDLoader: any;

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

    private initializeWokrbechFetching() {
        configAMD();
        AMDLoader.global.require(['vs/workbench/workbench.web.api'], (_: any) => {});
    }

    private async ensurePrivatePreviewUser() {
        let isAuthenticated = false;
        try {
            await EnvRegService.fetchEnvironments();
            isAuthenticated = true;
        } catch (e) {
            if (e.code === 401) {
                isAuthenticated = false;
            }
        }

        this.setState({
            isAuthenticated,
        });
    }

    async componentWillMount() {
        const token = await authService.getCachedToken();

        if (token) {
            this.ensurePrivatePreviewUser()
        }
    }

    render() {
        const { loading, isAuthenticated } = this.state;

        if (!isAuthenticated) {
            return <Redirect to='/welcome' />;
        }

        if (loading) {
            return <Loader message='Loading...' />;
        }

        this.initializeWokrbechFetching();

        return (
            <Fragment>
                <TitleBar />
                <EnvironmentsPanel />
            </Fragment>
        );
    }
}
