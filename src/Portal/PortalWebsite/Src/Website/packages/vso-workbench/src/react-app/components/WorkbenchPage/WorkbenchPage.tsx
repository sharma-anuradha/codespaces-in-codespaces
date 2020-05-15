import * as React from 'react';
import {
    EnvironmentStateInfo,
    createTrace,
    timeConstants
} from 'vso-client-core';

import { EnvironmentWorkspaceState } from '../../../interfaces/EnvironmentWorkspaceState';
import { TEnvironmentState } from '../../../interfaces/TEnvironmentState';
import { getEnvironmentInfo } from './utils/getEnvironmentInfo';
import { IWorkbenchStateObject } from './IWorkbenchStateObject';
import { startEnvironment } from './utils/startEnvironment';
import { config } from '../../../config/config';
import { WorkbenchPageRender } from './WorkbenchPageRender';
import { errorToState } from './errorToState';
import { authService } from '../../../auth/authService';
import { sendTelemetry } from '../../../telemetry/sendTelemetry';
import { getWelcomeMessage } from '../../../utils/getWelcomeMessage';

const { SECOND_MS } = timeConstants;

const trace = createTrace('workbench');

export class WorkbenchPage extends React.Component<{}, IWorkbenchStateObject> {
    private interval: ReturnType<typeof setInterval> | undefined;

    constructor(props: any, state: TEnvironmentState) {
        super(props, state);

        this.state = { value: EnvironmentWorkspaceState.Unknown };
        this.startPollingEnvironment();
    }

    private startPollingEnvironment = (interval = 2 * SECOND_MS) => {
        this.stopPollEnvironment();
        this.interval = setInterval(this.pollEnvironment, interval);
    };

    private stopPollEnvironment = () => {
        if (this.interval) {
            trace.info(`[polling]: stop`);
            clearInterval(this.interval);
        }
    };

    private startEnvironment = async () => {
        trace.info(`Environment in shutdown state, starting.`);

        await startEnvironment(this.setState.bind(this), this.handleAPIError, trace.info);

        this.setState({
            value: EnvironmentStateInfo.Starting,
        });

        this.startPollingEnvironment();
    };

    private pollEnvironment = async () => {
        try {
            trace.info(`[polling]: getting environment info.`);

            let environmentInfo = await getEnvironmentInfo(
                this.setState.bind(this),
                this.handleAPIError
            );

            if (!environmentInfo) {
                this.stopPollEnvironment();
                return;
            }

            trace.info(`[polling]: environment state -> ${environmentInfo.state}`);

            this.setState({
                environmentInfo,
                value: environmentInfo.state,
            });
        } catch (e) {
            this.handleAPIError(e);
        }
    };

    private handleAPIError = (e: Error) => {
        sendTelemetry('vsonline/workbench/error', e);

        const newState = errorToState(e);

        this.setState({ ...newState });

        const { value } = newState;
        if (
            value === EnvironmentWorkspaceState.SignedOut ||
            value === EnvironmentWorkspaceState.Error
        ) {
            this.stopPollEnvironment();
        }
    };

    private logWelcomeMessage = () => {
        console.log(getWelcomeMessage(config.environment, `${process.env.VSCS_WORKBENCH_VERSION}`));
    };

    public async componentDidMount() {
        try {
            this.setState({ value: EnvironmentWorkspaceState.Initializing });

            trace.info(`Getting config..`);

            await config.fetch();

            this.logWelcomeMessage();

            trace.info(`Getting environment info..`);

            const environmentInfo = await getEnvironmentInfo(
                this.setState.bind(this),
                this.handleAPIError
            );
            if (!environmentInfo) {
                /**
                 * error state already handled by
                 * the `getEnvironmentInfo` function
                 */
                return;
            }

            /**
             * Check if we need to autostart the environment.
             */
            const params = new URLSearchParams(location.search);
            const isShutdown = environmentInfo.state === EnvironmentStateInfo.Shutdown;
            if (params.get('autoStart') !== 'false' && isShutdown) {
                return await this.startEnvironment();
            }

            authService.onEvent((event) => {
                if (event === 'signed-out') {
                    this.setState({ value: EnvironmentWorkspaceState.SignedOut });
                }
            });

            authService.keepUserAuthenticated();

            this.setState({ value: environmentInfo.state });
        } catch (e) {
            this.handleAPIError(e);
        }
    }

    public render() {
        const { value, message } = this.state;

        trace.info(`render state: ${value}`);

        if (
            value !== EnvironmentStateInfo.Starting &&
            value !== EnvironmentStateInfo.ShuttingDown
        ) {
            this.stopPollEnvironment();
        }

        return (
            <WorkbenchPageRender
                state={value}
                message={message}
                startEnvironment={this.startEnvironment}
                handleAPIError={this.handleAPIError}
                onSignIn={authService.redirectToLogin}
            />
        );
    }
}
