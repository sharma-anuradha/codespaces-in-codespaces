import * as React from 'react';
import {
    EnvironmentStateInfo,
    createTrace,
    timeConstants,
    IEnvironment
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

export const isAutoStart = () => {
    const params = new URLSearchParams(location.search);
    return params.get('autoStart') !== 'false';
}

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

    private environmentInfo: IEnvironment | null = null;

    private pollEnvironment = async () => {
        try {
            trace.info(`[polling]: getting environment info.`);

            this.environmentInfo = await getEnvironmentInfo(
                this.setState.bind(this),
                this.handleAPIError
            );

            if (!this.environmentInfo) {
                this.stopPollEnvironment();
                return;
            }

            trace.info(`[polling]: environment state -> ${this.environmentInfo.state}`);

            this.setState({
                environmentInfo: this.environmentInfo,
                value: this.environmentInfo.state,
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
            const isShutdown = environmentInfo.state === EnvironmentStateInfo.Shutdown;
            if (isAutoStart() && isShutdown) {
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
            value !== EnvironmentStateInfo.Provisioning &&
            value !== EnvironmentStateInfo.ShuttingDown &&
            value !== EnvironmentWorkspaceState.Initializing &&
            value !== EnvironmentWorkspaceState.Unknown
        ) {
            this.stopPollEnvironment();
        }

        return (
            <WorkbenchPageRender
                environmentInfo={this.environmentInfo}
                environmentState={value}
                message={message}
                startEnvironment={this.startEnvironment}
                handleAPIError={this.handleAPIError}
                onSignIn={authService.redirectToLogin}
            />
        );
    }
}
