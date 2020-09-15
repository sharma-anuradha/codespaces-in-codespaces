import * as React from 'react';
import { PrimaryButton, Stack, Text } from 'office-ui-fabric-react';
import { VSOSplashScreen } from '@vs/vso-splash-screen'

import { isHostedOnGithub } from 'vso-client-core';
import { RenderSplashScreen, ConnectionAdapter } from 'vso-workbench';

import { BackToEnvironmentsLink } from '../back-to-environments/back-to-environments';
import { stateToDisplayName } from '../../utils/environmentUtils';
import { IWorkbenchSplashScreenProps } from '../../interfaces/IWorkbenchSplashScreenProps';
import { useTranslation } from 'react-i18next';
import { injectMessageParameters } from 'website/src/utils/injectMessageParameters';

interface IConnectSplashScreen {
    isOnVSCodespaces: boolean;
    title: string;
    buttonText: string;
    onConnect?:() => void;
}

export const ConnectSplashScreen: React.FunctionComponent<IConnectSplashScreen> = (props) => {
    return (
        <RenderSplashScreen isLightTheme={!props.isOnVSCodespaces} isLogo={isHostedOnGithub()}>
            <Stack
                    className={'connection-stack'}
                    horizontalAlign='center'
                    verticalFill
                    verticalAlign='center'
                    tokens={{ childrenGap: '20' }}>
                <Stack.Item>
                    <Text className='text-color'>{props.title}</Text>
                </Stack.Item>
                <Stack.Item>
                    <PrimaryButton onClick={props.onConnect}>{props.buttonText}</PrimaryButton></Stack.Item>
                {props.isOnVSCodespaces
                ? <Stack.Item>
                    <BackToEnvironmentsLink />
                  </Stack.Item>
                : <></>}
            </Stack>
        </RenderSplashScreen>
    );
}

export const WorkbenchSplashScreen: React.FC<IWorkbenchSplashScreenProps> = (props: IWorkbenchSplashScreenProps) => {
    const connection = React.useMemo(() => { return new ConnectionAdapter() }, []);
    const { showPrompt, environment, connectError, onRetry, onConnect } = props;
    const { friendlyName } = environment;
    const isOnVSCodespaces = !isHostedOnGithub();
    const { t: translation } = useTranslation();

    if (connectError !== null) {
        return (
            <ConnectSplashScreen
            isOnVSCodespaces={isOnVSCodespaces}
                title={injectMessageParameters(translation('connectingFailed'), friendlyName, connectError)}
                buttonText={translation('retry')}
                onConnect={onRetry}
                >
            </ConnectSplashScreen>
        );
    }
    
    const envState = stateToDisplayName(environment!.state, translation).toLocaleLowerCase();
    if (showPrompt) {
        const title = injectMessageParameters(translation('codespaceIsAtState'), friendlyName, envState)
        return (
            <ConnectSplashScreen
                isOnVSCodespaces={isOnVSCodespaces}
                title={title}
                buttonText={translation('connect')}
                onConnect={onConnect}
                >
            </ConnectSplashScreen>
        );
    } else {
        return (
                <RenderSplashScreen isLightTheme={!isOnVSCodespaces} isLogo={isHostedOnGithub()}>
                    <VSOSplashScreen connection={connection} github={!isOnVSCodespaces}></VSOSplashScreen>
                </RenderSplashScreen>
        );
    }

}