import React, { Component } from 'react';
import { connect } from 'react-redux';
import { RouteComponentProps } from 'react-router-dom';
import './workbench.css';

import { trace } from '../../utils/trace';
import { WEB_EMBED_PRODUCT_JSON, WEB_EMBED_PACKAGE_JSON } from '../../constants';

import { VSLSWebSocket, IWebSocketFactory } from '../../resolvers/vslsResolver';
import { IToken } from '../../services/authService';

import { ApplicationState } from '../../reducers/rootReducer';
import { ILocalCloudEnvironment, ICloudEnvironment } from '../../interfaces/cloudenvironment';
import { isEnvironmentAvailable } from '../../utils/environmentUtils';

export interface WorkbenchProps extends RouteComponentProps<{ id: string }> {
    token: IToken | undefined;
    environmentInfo: ILocalCloudEnvironment | undefined;
}

class WorkbenchView extends Component<WorkbenchProps> {
    // Since we have external scripts running outside of react scope,
    // we'll mange the instantiation flag outside of state as well.
    private workbenchMounted: boolean = false;

    componentDidUpdate() {
        const { environmentInfo } = this.props;
        if (!isEnvironmentAvailable(environmentInfo)) {
            return;
        }

        this.mountWorkbench(environmentInfo);
    }

    async componentDidMount() {
        const { environmentInfo } = this.props;
        if (!isEnvironmentAvailable(environmentInfo)) {
            return;
        }

        this.mountWorkbench(environmentInfo);
    }

    mountWorkbench(environmentInfo: ICloudEnvironment) {
        if (this.workbenchMounted) {
            return;
        }

        this.workbenchMounted = true;

        AMDLoader.global.require(['vs/workbench/workbench.web.api'], (workbench: any) => {
            const { sessionPath } = environmentInfo.connection;
            const { accessToken } = this.props.token!;

            const VSLSWebSocketFactory: IWebSocketFactory = {
                create(url: string) {
                    return new VSLSWebSocket(url, accessToken, environmentInfo);
                },
            };

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
                connectionToken: WEB_EMBED_PRODUCT_JSON.commit,
                productConfiguration: { 
                    version  : WEB_EMBED_PACKAGE_JSON.version, 
                    ...WEB_EMBED_PRODUCT_JSON 
                },
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

const getProps = (state: ApplicationState, props: RouteComponentProps<{ id: string }>) => {
    const environmentInfo = state.environments.environments.find((e) => {
        return e.id === props.match.params.id;
    });

    return {
        token: state.authentication.token,
        environmentInfo,
    };
};

export const Workbench = connect(getProps)(WorkbenchView);
