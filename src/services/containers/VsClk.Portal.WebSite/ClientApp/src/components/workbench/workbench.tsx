import React, { Component } from 'react';
import { connect } from 'react-redux';
import { RouteComponentProps } from 'react-router-dom';
import './workbench.css';

import { trace } from '../../utils/trace';
import { WEB_EMBED_PRODUCT_JSON } from '../../constants';

import { VSLSWebSocket, IWebSocketFactory } from '../../resolvers/vslsResolver';
import { IToken } from '../../services/authService';
import envRegService from '../../services/envRegService';

import { amdConfig } from '../../amd/amdConfig';
import { ReduxAuthenticationProvider } from '../../actions/reduxAuthenticationProvider';
import { Dispatch } from '../../actions/actionUtils';
import { ApplicationState } from '../../reducers/rootReducer';

declare var AMDLoader: any;

export interface WorkbenchProps extends RouteComponentProps {
    dispatch: Dispatch;
    token: IToken | undefined;
}

export interface WorkbenchState {
    isLoading?: boolean;
}

class WorkbenchView extends Component<WorkbenchProps, WorkbenchState> {
    constructor(props: WorkbenchProps) {
        super(props);

        this.state = {};
    }

    async componentDidMount() {
        amdConfig();

        new ReduxAuthenticationProvider(this.props.dispatch);
        const environmentId = location.href.split('environment/')[1];

        const authProvider = new ReduxAuthenticationProvider(this.props.dispatch);

        const environmentInfo = await envRegService.getEnvironment(environmentId, authProvider);
        if (!environmentInfo) {
            return;
        }

        trace(`Environment info: `, environmentInfo);

        const { sessionPath } = environmentInfo.connection;
        const { accessToken } = this.props.token!;

        AMDLoader.global.require(['vs/workbench/workbench.web.api'], (workbench: any) => {
            const VSLSWebSocketFactory: IWebSocketFactory = new (class
                implements IWebSocketFactory {
                create(url: string) {
                    return new VSLSWebSocket(url, accessToken, environmentInfo);
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
                connectionToken: WEB_EMBED_PRODUCT_JSON.commit
            };

            trace(`Creating workbench on #${this.workbenchRef}, with config: `, config);

            workbench.create(this.workbenchRef, config);
        });
    }

    private workbenchRef: HTMLDivElement | null = null;

    render() {
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

const getAuthToken = ({ authentication: { token } }: ApplicationState) => ({
    token,
});

export const Workbench = connect(getAuthToken)(WorkbenchView);
