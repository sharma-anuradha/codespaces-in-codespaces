import * as React from 'react';

import { createTrace } from 'vso-client-core';

import { authService } from '../../../auth/authService';
import { config } from '../../../config/config';
import { Workbench } from '../../../vscode/workbenches/defaultWorkbench';

import './Workbench.css';

export interface IWorkbechPropsComponent {
    onError: (e: Error) => any;
    onConnection?: () => any;
    onMount?: () => any;
}

const trace = createTrace('workbench');

class WorkbenchComponent extends React.Component<IWorkbechPropsComponent> {
    private readonly domElementId = 'js-vscode-workbench-placeholder';

    private workbench: Workbench | null = null;

    public shouldComponentUpdate() {
        return false;
    }

    private onConnection = async () => {
        const {
            onConnection = () => {},
            onMount = () => {},
        } = this.props;

        onConnection();

        if (!this.workbench) {
            throw new Error('No VSCode Workbench initialized.');
        }

        await this.workbench.mount();

        onMount();
    };

    async componentDidMount() {
        trace.info(`Getting config..`);
        await config.fetch();

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
            <div
                id={this.domElementId}
                className='vso-workbench-root'
            />
        );
    }
}

export { WorkbenchComponent as Workbench };
