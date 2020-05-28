import * as React from 'react';
import { VSOSplashScreen } from '@vs/vso-splash-screen'

import {
    ILocalEnvironment, randomString,
} from 'vso-client-core';

import { ConnectionAdapter } from './ConnectionAdapter';
import { RenderSplashScreen } from './RenderSplashScreen';

import './SplashScreenShell.css';

export interface IWorkbenchSplashScreenProps {
    environment?: ILocalEnvironment;
    connectError?: string;
    onRetry?: () => void;
    onConnect?: () => void;
    isGithubSplashScreen: boolean;
}

export const SplashScreenShell: React.FC<IWorkbenchSplashScreenProps> = (props: IWorkbenchSplashScreenProps) => {
    const connection = React.useMemo(() => { return new ConnectionAdapter() }, []);

    const {
        isGithubSplashScreen,
    } = props;

    return (
        <RenderSplashScreen isOnVSCodespaces={!isGithubSplashScreen}>
            <VSOSplashScreen
                connection={connection}
                github={isGithubSplashScreen}
            />
        </RenderSplashScreen>
    );
}
