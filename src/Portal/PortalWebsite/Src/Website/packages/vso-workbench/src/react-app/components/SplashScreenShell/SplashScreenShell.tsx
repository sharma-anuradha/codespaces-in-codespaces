import * as React from 'react';
import { VSOSplashScreen } from '@vs/vso-splash-screen'

import {
    ILocalEnvironment, isHostedOnGithub
} from 'vso-client-core';

import { ConnectionAdapter } from './ConnectionAdapter';
import { RenderSplashScreen } from './RenderSplashScreen';

import './SplashScreenShell.css';

export interface IWorkbenchSplashScreenProps {
    environment?: ILocalEnvironment;
    connectError?: string;
    isLightTheme: boolean;
    onRetry?: () => void;
    onConnect?: () => void;
}

export const SplashScreenShell: React.FC<IWorkbenchSplashScreenProps> = (props: IWorkbenchSplashScreenProps) => {
    const connection = React.useMemo(() => { return new ConnectionAdapter() }, []);

    const {
        isLightTheme,
    } = props;

    return (
        <RenderSplashScreen isLightTheme={isLightTheme} isLogo={isHostedOnGithub()}>
            <VSOSplashScreen
                connection={connection}
                github={isLightTheme}
            />
        </RenderSplashScreen>
    );
}
