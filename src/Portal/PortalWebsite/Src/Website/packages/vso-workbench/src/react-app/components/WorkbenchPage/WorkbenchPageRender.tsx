import * as React from 'react';

import { EnvironmentStateInfo, wait, IEnvironment } from 'vso-client-core';

import { Workbench } from '../Workbench/Workbench';
import { TEnvironmentState } from '../../../interfaces/TEnvironmentState';
import { Fragment } from 'react';
import { SplashScreenState } from './SplashScreenState';

export interface IWorkbenchPageRenderProps {
    environmentInfo: IEnvironment | null;
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
export class WorkbenchPageRender extends React.Component<IWorkbenchPageRenderProps, IWorkbenchPageRenderState> {
    constructor(
        props: IWorkbenchPageRenderProps,
        state: IWorkbenchPageRenderState
    ) {
        super(props, state);

        this.state = { isMounted: false };
    }

    private onMount = () => {
        /**
         * Give the VSCode Workbench a chance to render before removing the Splash Screen.
         */
        setTimeout(() => {
            this.setState({ isMounted: true });
        }, 200);
    }

    public render() {
        return (
            <Fragment>
                { this.getSplashScreen() }
                { this.getWorkbench() }
            </Fragment>
        );
    }

    private getWorkbench = () => {
        const {
            environmentState,
            handleAPIError,
        } = this.props;

        if (environmentState === EnvironmentStateInfo.Available) {
            return (
                <Workbench
                    onError={handleAPIError}
                    onMount={this.onMount}
                />
            );
        }

        return null;
    }

    private getSplashScreen = () => {
        // hide the splash screen when the workbench gets mounted
        if (this.state.isMounted) {
            return null;
        }
        
        return <SplashScreenState {...this.props} />;
    }
}
