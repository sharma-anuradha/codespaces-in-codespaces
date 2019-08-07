import React, { Component } from 'react';
import { RouteComponentProps, Redirect } from 'react-router';
import './workbench.css';

import { trace } from '../../utils/trace';
import { WEB_EMBED_PRODUCT_JSON } from '../../constants';

import { VSLSWebSocket, IWebSocketFactory } from '../../resolvers/vslsResolver';
import { authService } from '../../services/authService';
import envRegService from '../../services/envRegService';

import { amdConfig } from '../../amd/amdConfig';

declare var AMDLoader: any;

export interface WorkbenchProps extends RouteComponentProps {}

export interface WorkbenchState {
    isLoading?: boolean;
    isAuthenticated?: boolean;
}

export class Workbench extends Component<WorkbenchProps, WorkbenchState> {
    constructor(props: WorkbenchProps) {
        super(props);

        this.state = {
            isAuthenticated: true,
        };
    }

    async componentDidMount() {
        const aadToken = await authService.getCachedToken();

        if (!aadToken) {
            this.setState({
                isAuthenticated: false,
            });

            return;
        }

        amdConfig();

        const environmentId = location.href.split('environment/')[1];
        const environmentInfo = await envRegService.getEnvironment(environmentId);

        trace(`Environment info: `, environmentInfo);

        const { sessionPath } = environmentInfo.connection;

        AMDLoader.global.require(['vs/workbench/workbench.web.api'], (workbench: any) => {
            const VSLSWebSocketFactory: IWebSocketFactory = new (class
                implements IWebSocketFactory {
                create(url: string) {
                    return new VSLSWebSocket(url, aadToken.accessToken, environmentInfo);
                }
            })();

            const config = {
                folderUri: {
                    $mid: 1,
                    path: sessionPath,
                    scheme: 'vscode-remote',
                    authority: `localhost`,
                },
                remoteAuthority: `localhost`,
                webviewEndpoint: `http://localhost`,
                webSocketFactory: VSLSWebSocketFactory,
            };

            trace(`Creating workbench on #${this.workbenchRef}, with config: `, config);

            workbench.create(this.workbenchRef, config);
        });
    }

    private renderSplashScreen() {
        return (
            <div className='vsonline-workbench__splash-screen'>
                <p className='vsonline-workbench__splash-screen-caption'>Getting things ready..</p>
            </div>
        );
    }

    private workbenchRef: HTMLDivElement | null = null;

    render() {
        const { isAuthenticated } = this.state;

        if (!isAuthenticated) {
            return <Redirect to='/welcome' />;
        }

        return (
            <div className='vsonline-workbench'>
                <meta
                    id='vscode-remote-product-configuration'
                    data-settings={JSON.stringify(WEB_EMBED_PRODUCT_JSON)}
                />
                <div
                    id='workbench'
                    style={{ height: '100%' }}
                    ref={
                        // tslint:disable-next-line: react-this-binding-issue
                        (el) => (this.workbenchRef = el)
                    }
                />
            </div>
        );
    }
}
