import React, { Component, Fragment } from 'react';
import { Redirect, RouteComponentProps } from 'react-router';

import './main.css';

import { TitleBar } from '../titlebar/titlebar';
import { Navigation } from '../navigation/navigation';
import { Loader } from '../loader/loader';

import { EnvironmentsPanel } from '../environmentsPanel/environments-panel';
import { authService } from '../../services/authService';
import { amdConfig } from '../../amd/amdConfig';
import envRegService from '../../services/envRegService';

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

    private initializeWorkbenchFetching() {
        if (amdConfig()) {
            AMDLoader.global.require(['vs/workbench/workbench.web.api'], (_: any) => {});
        }
    }

    private async ensurePrivatePreviewUser() {
        let isAuthenticated = false;
        try {
            await envRegService.fetchEnvironments();
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
            this.ensurePrivatePreviewUser();
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

        this.initializeWorkbenchFetching();

        return (
            <div className='ms-Grid main'>
                <div className='ms-Grid-row'>
                    <TitleBar />
                </div>
                <div className='ms-Grid-row main__app-content'>
                    <div className='ms-Grid-col ms-bgColor-gray20 main__app-navigation-container'>
                        <Navigation />
                    </div>
                    <div className='ms-Grid-col main__app-content-container'>
                        <EnvironmentsPanel />
                    </div>
                </div>
            </div>
        );
    }
}
