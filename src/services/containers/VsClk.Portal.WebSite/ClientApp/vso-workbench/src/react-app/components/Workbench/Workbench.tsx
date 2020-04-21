import * as React from 'react';
import { Fragment } from 'react';

import { authService } from '../../../auth/authService';
import { config } from '../../../config/config';
import { Workbench } from '../../../vscode/workbenches/defaultWorkbench';

import './Workbench.css';
import { SplashScreenMessage } from '../SplashScreenShellMessage/SplashScreenShellMessage';

export interface IWorkbechPropsComponent {
    onError: (e: Error) => any | Promise<any>;
}

class WorkbenchComponent extends React.Component<IWorkbechPropsComponent> {
    private readonly domElementId = 'js-vscode-workbench-placeholder';

    private workbench: Workbench | null = null;

    public shouldComponentUpdate() {
        return false;
    }

    private onConnection = async () => {
        if (!this.workbench) {
            throw new Error('No VSCode Workbench initialized.');
        }

        await this.workbench.mount();
    };

    async componentDidMount() {
        this.workbench = new Workbench({
            domElementId: this.domElementId,
            getToken: authService.getCachedToken,
            liveShareEndpoint: config.liveShareApi,
            onConnection: this.onConnection,
            onError: this.props.onError,
            enableEnvironmentPortForwarding: config.enableEnvironmentPortForwarding,
            portForwardingDomainTemplate: config.portForwardingDomainTemplate,
        });

        await this.workbench.connect();
    }

    render() {
        return (
            <Fragment>
                <SplashScreenMessage message='Connecting...' isSpinner={true} />

                <div className='vso-workbench-root' id={this.domElementId} />
            </Fragment>
        );
    }
}

export { WorkbenchComponent as Workbench };
