import * as React from 'react';
import classnames from 'classnames';
import {
    EnvironmentStateInfo,
    createTrace,
    timeConstants,
    IEnvironment,
    setCurrentCodespaceId,
    IPartnerInfo,
} from 'vso-client-core';

import { EnvironmentWorkspaceState } from '../../../interfaces/EnvironmentWorkspaceState';
import { TEnvironmentState } from '../../../interfaces/TEnvironmentState';
import { getEnvironmentInfo } from './utils/getEnvironmentInfo';
import { IWorkbenchStateObject } from './IWorkbenchStateObject';
import { config } from '../../../config/config';
import { WorkbenchPageRender } from './WorkbenchPageRender';
import { errorToState } from './errorToState';
import { authService } from '../../../auth/authService';
import { sendTelemetry } from '../../../telemetry/sendTelemetry';
import { getWelcomeMessage } from '../../../utils/getWelcomeMessage';
import { vsoAPI } from '../../../api/vsoAPI';
import { VSCodespacesPlatformInfo } from 'vs-codespaces-authorization';
import { PlatformQueryParams, CONNECT_ATTEMPT_COUNT_LS_KEY } from '../../../constants';
import { getQueryParamFlag, setQueryParamFlag } from '../../../utils/queryParamFlag';
import { MaybeDevPanel } from './DevPanel';

import { LOADING_ENVIRONMENT_STAGE } from './DevPanelHeader';
import { assertValidSubdomain } from '../../../utils/assertValidSubdomain';

import './WorkbenchPage.css';
import { FeatureFlags, featureFlags } from '../../../config/featureFlags';

const trace = createTrace('workbench');

const { SECOND_MS } = timeConstants;

interface IWorkbenchPageProps {
    platformInfo: IPartnerInfo | VSCodespacesPlatformInfo | null;
}

export class WorkbenchPage extends React.Component<IWorkbenchPageProps, IWorkbenchStateObject> {
    private interval: ReturnType<typeof setInterval> | undefined;

    constructor(props: any, state: TEnvironmentState) {
        super(props, state);

        this.state = {
            value: EnvironmentWorkspaceState.Unknown,
        };
        this.startPollingEnvironment();
    }

    private startPollingEnvironment = async (interval = 2 * SECOND_MS) => {
        this.stopPollEnvironment();

        const info = await authService.getPartnerInfo();
        if (info) {
            const id = 'environmentId' in info ? info.environmentId : info.codespaceId;

            setCurrentCodespaceId(id);
        }

        this.interval = setInterval(this.pollEnvironment, interval);
    };

    private stopPollEnvironment = () => {
        if (this.interval) {
            trace.info(`[polling]: stop`);
            clearInterval(this.interval);
        }
    };

    /**
     * Since GitHub needs to have the control over the codespace permissions,
     * we need to call their API so they can perevent the codespave from being
     * started for offboarded or blocked users.
     */
    private startCodespace = async () => {
        this.setState({
            value: EnvironmentStateInfo.Starting,
        });

        trace.info(`Environment in shutdown state, starting.`);

        if (!this.environmentInfo) {
            throw new Error(`Fetch environment info first.`);
        }

        // stop polling so the splash screen
        // does not flicker during the start call
        await this.stopPollEnvironment();

        trace.info(`Starting codespace`);

        try {
            await vsoAPI.startCodespace(this.environmentInfo);
        } catch (e) {
            trace.error(`Failed to start Codespace: `, e);
            return this.handleAPIError(e);
        }

        await this.startPollingEnvironment();
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

            assertValidSubdomain(this.environmentInfo);

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

    public async componentWillMount() {
        try {
            this.setState({ value: EnvironmentWorkspaceState.Initializing });

            trace.info(`Getting config..`);

            await config.fetch();

            this.logWelcomeMessage();

            /**
             * Check if we need to auto authorize to the environment.
             */
            const isAuthorized = !!(await authService.getPartnerInfo());
            if ((await getQueryParamFlag(PlatformQueryParams.AutoAuthRedirect)) && !isAuthorized) {
                /**
                 * Since we redirect for the credentials to external partners,
                 * if something unexpected happens, there is a potential to stuck in an infinite loop.
                 * The logic below aimed to break such loop after sequential failed 3 attempts.
                 */
                const connectAttempLsValue =
                    localStorage.getItem(CONNECT_ATTEMPT_COUNT_LS_KEY) || '';
                const connectAttemptCount = parseInt(connectAttempLsValue, 10) || 0;

                // too many attempts, bail out of the OAuth redirection infinite loop
                if (connectAttemptCount >= 3) {
                    return this.setState({
                        value: EnvironmentWorkspaceState.SignedOut,
                        message: 'Cannot connect to the Codespace.',
                    });
                }

                // increment attempt count
                localStorage.setItem(CONNECT_ATTEMPT_COUNT_LS_KEY, `${connectAttemptCount + 1}`);

                return await authService.redirectToLogin();
            }
            // on successful auth, reset the count
            localStorage.removeItem(CONNECT_ATTEMPT_COUNT_LS_KEY);

            authService.onEvent((event) => {
                if (event === 'signed-out') {
                    // if the Codespace is inactive, set the flag
                    // to prevent auto redirection to auth endoint
                    setQueryParamFlag(PlatformQueryParams.AutoAuthRedirect, false);
                    this.setState({ value: EnvironmentWorkspaceState.SignedOut });
                }
            });

            authService.keepUserAuthenticated();

            trace.info(`Getting environment info..`);

            this.environmentInfo = await getEnvironmentInfo(
                this.setState.bind(this),
                this.handleAPIError
            );
            if (!this.environmentInfo) {
                /**
                 * error state already handled by
                 * the `getEnvironmentInfo` function
                 */
                return;
            }

            assertValidSubdomain(this.environmentInfo);

            /**
             * Check if we need to auto start the environment.
             */
            const isShutdown = this.environmentInfo.state === EnvironmentStateInfo.Shutdown;
            const isAutoStart = await getQueryParamFlag(PlatformQueryParams.AutoStart);
            if (isAutoStart && isShutdown) {
                return await this.startCodespace();
            }

            this.setState({ value: this.environmentInfo.state });
        } catch (e) {
            this.handleAPIError(e);
        }
    }

    private isDevPanel = (
        codespaceInfo: IPartnerInfo | VSCodespacesPlatformInfo | null
    ): boolean => {
        if (!config.isFetched) {
            return false;
        }

        const isNonProd = config.environment !== 'production';
        if (!codespaceInfo) {
            return isNonProd;
        }

        return featureFlags.isEnabledInPayload(
            (codespaceInfo as VSCodespacesPlatformInfo).featureFlags,
            FeatureFlags.Developer,
            isNonProd,
        );
    };

    public render() {
        const { platformInfo } = this.props;
        const { value, message } = this.state;

        trace.info(`render state: ${value}`);

        if (
            value !== EnvironmentStateInfo.Queued &&
            value !== EnvironmentStateInfo.Starting &&
            value !== EnvironmentStateInfo.Provisioning &&
            value !== EnvironmentStateInfo.ShuttingDown &&
            value !== EnvironmentWorkspaceState.Initializing &&
            value !== EnvironmentWorkspaceState.Unknown
        ) {
            this.stopPollEnvironment();
        }

        if (!platformInfo) {
            this.stopPollEnvironment();
        }

        const isDevPanel = this.isDevPanel(platformInfo);
        const className = classnames('vscs-workbench-page', {
            'is-dev-panel': isDevPanel,
        });

        const environment = config.isFetched
            ? config.environment
            : LOADING_ENVIRONMENT_STAGE;

        return (
            <div className={className}>
                <MaybeDevPanel
                    className='vscs-workbench-page__dev-panel'
                    codespaceInfo={platformInfo}
                    isDevPanel={isDevPanel}
                    environment={environment}
                />
                <WorkbenchPageRender
                    className='vscs-workbench-page__body'
                    environmentInfo={this.environmentInfo}
                    platformInfo={platformInfo}
                    environmentState={value}
                    message={message}
                    startEnvironment={this.startCodespace}
                    handleAPIError={this.handleAPIError}
                    onSignIn={async () => {
                        localStorage.removeItem(CONNECT_ATTEMPT_COUNT_LS_KEY);
                        await authService.redirectToLogin();
                    }}
                />
            </div>
        );
    }
}
