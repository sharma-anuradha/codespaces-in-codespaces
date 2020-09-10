import * as React from 'react';

import { EnvironmentStateInfo, wait, IEnvironment, IPartnerInfo } from 'vso-client-core';

import { Workbench } from '../Workbench/Workbench';
import { TEnvironmentState } from '../../../interfaces/TEnvironmentState';
import { SplashScreenState } from './SplashScreenState';
import { telemetryMarks } from '../../../telemetry/telemetryMarks';
import { VSCodespacesPlatformInfo } from 'vs-codespaces-authorization';
import { removeDefaultSplashScreen } from './utils/removeDefaultSplashScreen';
import { credentialsProvider } from '../../../vscode/providers/credentialsProvider/credentialsProvider';
import { ServerlessSplashscreen } from '../ServerlessSplashscreen/ServerlessSplashscreen';
import { featureFlags, FeatureFlags } from 'vso-workbench/src/config/featureFlags';
import { authService } from 'vso-workbench/src/auth/authService';

export interface IWorkbenchPageRenderProps {
    className?: string;
    environmentInfo: IEnvironment | null;
    platformInfo: IPartnerInfo | VSCodespacesPlatformInfo | null;
    environmentState: TEnvironmentState;
    message?: string;
    startEnvironment: () => any;
    onSignIn: () => any;
    handleAPIError: (e: Error) => any;
}

interface IWorkbenchPageRenderState {
    isMounted: boolean;
    isServerlessSplashScreenShown: boolean;
}

/**
 * Component to render VSCode Workbench or Splash Screen.
 */
export class WorkbenchPageRender extends React.Component<
    IWorkbenchPageRenderProps,
    IWorkbenchPageRenderState
> {
    constructor(props: IWorkbenchPageRenderProps, state: IWorkbenchPageRenderState) {
        super(props, state);

        var isServerlessSplashScreenShown: boolean = false;
        if (
            featureFlags.isEnabled(FeatureFlags.ServerlessEnabled) &&
            this.props.environmentInfo?.state === EnvironmentStateInfo.Provisioning
        ) {
            isServerlessSplashScreenShown = true;
        }
        this.state = {
            isMounted: false,
            isServerlessSplashScreenShown: isServerlessSplashScreenShown,
        };
    }

    private onMount = () => {
        /**
         * Give the VSCode Workbench a chance to render before removing the Splash Screen.
         */
        setTimeout(() => {
            this.setState({ isMounted: true });
            requestAnimationFrame(removeDefaultSplashScreen);
        }, 200);
    };

    async componentDidUpdate(
        prevProps: Readonly<IWorkbenchPageRenderProps>,
        prevState: Readonly<IWorkbenchPageRenderState>
    ): Promise<void> {
        if (
            this.state.isServerlessSplashScreenShown &&
            this.props.environmentInfo?.state !== EnvironmentStateInfo.Provisioning
        ) {
            window.location.reload(true);
        }

        if (
            !this.state.isServerlessSplashScreenShown &&
            (await featureFlags.isEnabled(FeatureFlags.ServerlessEnabled)) &&
            this.props.environmentInfo?.state === EnvironmentStateInfo.Provisioning
        ) {
            this.setState({ isServerlessSplashScreenShown: true });
        }
    }

    public render() {
        const { className = '' } = this.props;

        return (
            <div className={className}>
                {this.getSplashScreen()}
                {this.getWorkbench()}
            </div>
        );
    }

    private getWorkbench = () => {
        const { environmentState, handleAPIError } = this.props;

        if (environmentState === EnvironmentStateInfo.Available) {
            if (window.performance.mark) {
                window.performance.mark(telemetryMarks.timeToInteractive);
            }
            return <Workbench onError={handleAPIError} onMount={this.onMount} />;
        }

        return null;
    };

    private getSplashScreen = () => {
        // hide the splash screen when the workbench gets mounted
        if (this.state.isMounted) {
            return null;
        }

        if (this.state.isServerlessSplashScreenShown) {
            return (
                <ServerlessSplashscreen
                    environment={this.props.environmentInfo}
                    credentialsProvider={credentialsProvider}
                    getGithubToken={authService.getCachedGithubToken}
                />
            );
        }

        requestAnimationFrame(removeDefaultSplashScreen);

        return <SplashScreenState {...this.props} platformInfo={this.props.platformInfo} />;
    };
}
