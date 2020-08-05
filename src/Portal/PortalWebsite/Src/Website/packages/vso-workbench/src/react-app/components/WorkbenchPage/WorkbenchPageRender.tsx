import * as React from 'react';

import { EnvironmentStateInfo, wait, IEnvironment, IPartnerInfo } from 'vso-client-core';

import { Workbench } from '../Workbench/Workbench';
import { TEnvironmentState } from '../../../interfaces/TEnvironmentState';
import { Fragment } from 'react';
import { SplashScreenState } from './SplashScreenState';
import { telemetryMarks } from '../../../telemetry/telemetryMarks';
import { VSCodespacesPlatformInfo } from 'vs-codespaces-authorization';
import { removeDefaultSplashScreen } from './utils/removeDefaultSplashScreen';

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

        this.state = { isMounted: false };
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

        requestAnimationFrame(removeDefaultSplashScreen);

        return <SplashScreenState {...this.props} platformInfo={this.props.platformInfo} />;
    };
}
