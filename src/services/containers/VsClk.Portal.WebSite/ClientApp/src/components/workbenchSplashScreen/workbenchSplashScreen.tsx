import * as React from 'react';
import { PrimaryButton, Stack, Text } from 'office-ui-fabric-react';
import { VSOSplashScreen, IConnectionAdapter } from '@vs/vso-splash-screen'

import { isHostedOnGithub } from 'vso-client-core';

import { BackToEnvironmentsLink } from '../back-to-environments/back-to-environments';
import { stateToDisplayName } from '../../utils/environmentUtils';
import { IWorkbenchSplashScreenProps } from '../../interfaces/IWorkbenchSplashScreenProps';
import './workbenchSplashScreen.css';
import classnames from 'classnames';

interface IRenderSplashScreenProps {
    isOnGithub: boolean;
}

interface IConnectSplashScreen {
    isOnGithub: boolean;
    title: string;
    buttonText: string;
    onConnect?:() => void;
}

class CommunicationProvider implements IConnectionAdapter {
    sendCommand(command: string, args: { environmentId: string }) {
        switch(command) {
            case 'connect':
                    window.postMessage({
                        command,
                        environmentId: args.environmentId,
                    }, window.origin);
                break;
        }
    }

    onMessage(callback: (ev: MessageEvent) => any) {
        window.addEventListener("message", callback, false);
    }
}

export const RenderSplashScreen: React.FunctionComponent<IRenderSplashScreenProps> = (props) => {
    const mainClass = classnames('vsonline-splash-screen-main', {'is-github': props.isOnGithub});
    return (
        <div className={mainClass}>
            <div className="vsonline-splash-screen-extensions-pane"></div>
            <div className='vsonline-splash-screen-tree-pane'></div>
            <div className="vsonline-splash-screen-editor">
                <div className='vsonline-splash-screen-titlebar'>
                    <div className='vsonline-splash-screen-titlebar-tab'></div>
                </div>
                <div className="vsonline-splash-screen-body">
                    {props.children}
                </div>
            </div>
        </div>
    );
};

export const ConnectSplashScreen: React.FunctionComponent<IConnectSplashScreen> = (props) => {
    return (
        <RenderSplashScreen isOnGithub={props.isOnGithub}>
            <Stack
                    horizontalAlign='center'
                    verticalFill
                    verticalAlign='center'
                    tokens={{ childrenGap: '20' }}>
                <Stack.Item>
                    <Text className='text-color'>{props.title}</Text>
                </Stack.Item>
                <Stack.Item>
                    <PrimaryButton onClick={props.onConnect}>Connect</PrimaryButton></Stack.Item>
                {!props.isOnGithub
                ? <Stack.Item>
                    <BackToEnvironmentsLink />
                  </Stack.Item>
                : <></>}
            </Stack>
        </RenderSplashScreen>
    );
}

export const WorkbenchSplashScreen: React.FC<IWorkbenchSplashScreenProps> = (props: IWorkbenchSplashScreenProps) => {
    const connection = React.useMemo(() => { return new CommunicationProvider() }, []);
    const { showPrompt, environment, connectError, onRetry, onConnect } = props;
    const { friendlyName } = environment;
    const isOnGithub = isHostedOnGithub();

    if (connectError !== null) {
        return (
            <ConnectSplashScreen
                isOnGithub={isOnGithub}
                title={`Connecting to environment ${friendlyName} failed. ${connectError}`}
                buttonText='Retry'
                onConnect={onRetry}
                >
            </ConnectSplashScreen>
        );
    }
    
    const envState = stateToDisplayName(environment!.state).toLocaleLowerCase();
    if (showPrompt) {
        const containerNaming = isOnGithub ? 'Workspace' : 'Environment';
        const title = `${containerNaming} "${friendlyName}" is ${envState}.`
        return (
            <ConnectSplashScreen
                isOnGithub={isOnGithub}
                title={title}
                buttonText='Connect'
                onConnect={onConnect}
                >
            </ConnectSplashScreen>
        );
    } else {
        return (
                <RenderSplashScreen isOnGithub={isOnGithub}>
                    <VSOSplashScreen connection={connection} github={isOnGithub}></VSOSplashScreen>
                </RenderSplashScreen>
        );
    }

}