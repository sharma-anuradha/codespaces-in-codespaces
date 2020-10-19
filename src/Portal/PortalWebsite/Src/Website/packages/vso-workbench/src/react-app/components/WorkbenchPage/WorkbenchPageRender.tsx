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
import { featureFlags, FeatureFlags } from '../../../config/featureFlags';
import { authService } from '../../../auth/authService';
import { EnvironmentWorkspaceState } from '../../../interfaces/EnvironmentWorkspaceState';

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
        const { isMounted, isServerlessSplashScreenShown } = this.state;
        const { environmentInfo, environmentState } = this.props;

        // When user gets signed out on the workbench page, we expect to transition back
        // to the splash screen which will render the `Signed Out` screen. As an alternative
        // we could reset the `isMounted` flag but current vscode workbench cannot be mounted
        // twice, so we will need a page reload anyways.
        // Note: other workbench states mgiht also need same level of treatment in the future.
        const isSignedOut = (environmentState === EnvironmentWorkspaceState.SignedOut);
        // hide the splash screen when the workbench gets mounted
        if (isMounted && !isSignedOut) {
            return null;
        }

        if (isServerlessSplashScreenShown) {
            return (
                <ServerlessSplashscreen
                    environment={environmentInfo}
                    credentialsProvider={credentialsProvider}
                    getGithubToken={authService.getCachedGithubToken}
                />
            );
        }

        requestAnimationFrame(removeDefaultSplashScreen);

        return <SplashScreenState {...this.props} platformInfo={this.props.platformInfo} />;
    };
}
