import React, { Component } from 'react';
import { Redirect } from 'react-router';
import { Spinner } from 'office-ui-fabric-react/lib/Spinner';
import { PrimaryButton } from 'office-ui-fabric-react/lib/Button';
import { Link } from 'office-ui-fabric-react/lib/Link';

import { EnvironmentCard } from '../environmentCard/environment-card';
import { authService } from '../../services/authService';
import envRegService from '../../services/envRegService';

import { ICloudEnvironment } from '../../interfaces/cloudenvironment';

import './environments-panel.css';


export interface EnvironmentsPanelProps {}
export interface EnvironmentsPanelState {
    environments: ICloudEnvironment[];
    isLoading: boolean;
    isAuthenticated: boolean;
}

export class EnvironmentsPanel extends Component<EnvironmentsPanelProps, EnvironmentsPanelState> {
    constructor(props: EnvironmentsPanelProps) {
        super(props);

        this.state = {
            environments: [],
            isLoading: true,
            isAuthenticated: true
        };

        authService.getCachedToken().then((token) => {
            if (token) {
                envRegService.fetchEnvironments()
                    .then((environments) => {
                        this.setState({
                            environments,
                            isLoading: false
                        });
                    }).catch((e) => {
                        if ((e.message.indexOf('401') !== -1) || (e.code === 401)) {
                            authService.signOut();
                            console.error('Please sign up!');

                            this.setState({ isAuthenticated: false });
                        }
                    });
            }
        });
    }

    private renderEnvironments() {
        const { isLoading, isAuthenticated, environments } = this.state;

        if (!isAuthenticated) {
            return (<Redirect to='/welcome' />);
        }

        if (isLoading) {
            return (
                <Spinner
                    className='environments-panel__environments-spinner'
                    label="Fetching your environments..."
                    ariaLive="assertive"
                    labelPosition="right" />
                );
        }


        const envs = [];
        let i = 0;
        for (let env of environments.slice(0, 3)) {
            envs.push(
                <div className='ms-Grid-col ms-sm6 ms-md4 ms-lg4' key={i++}>
                    <EnvironmentCard environment={env} id={i} />
                </div>
            );   
        }

        return (
            <div className='ms-Grid-row'>
                <div className='ms-Grid-row environments-panel__environments'>
                    {
                        (envs.length)
                            ? envs
                            : this.renderNoEnvironments()
                    }
                </div>
                {
                    (envs.length)
                        ? this.renderViewAll()
                        : null
                }
            </div>
        );
    }

    private renderNoEnvironments() {
        return (
            <div
                className='environments-panel__no-environments'
                key='no-envs'>
                    No enviroments so far. <Link>Create one!</Link>
            </div>
        );
    }

    private renderViewAll() {
        return (
            <div className='ms-Grid' dir='ltr'>
                <div className='ms-Grid-row'>
                    <div className='ms-Grid-col ms-sm6 ms-md4 ms-lg10'></div>
                    <div className='ms-Grid-col ms-sm6 ms-md8 ms-lg2 environments-panel__tar'>
                        <Link className='environments-panel__show-all-environments'>
                            View all my environments
                        </Link>
                    </div>
                </div>
            </div>
        );
    }

    render() {
        return (
            <div className='environments-panel'>
                <div className='ms-Grid' dir='ltr'>
                    <div className='ms-Grid-row'>
                        <div className='ms-Grid-col ms-sm6 ms-md4 ms-lg9'></div>
                        <div className='ms-Grid-col ms-sm6 ms-md8 ms-lg3 environments-panel__tar'>
                            <PrimaryButton
                                text='Create environment'
                                className='environments-panel__create-button'
                                />
                        </div>
                    </div>
                    {this.renderEnvironments()}
                </div>
            </div>
        );
    }
}
