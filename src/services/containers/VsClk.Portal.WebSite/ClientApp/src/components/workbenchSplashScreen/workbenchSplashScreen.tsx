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
    isOnVSCodespaces: boolean;
}

interface IConnectSplashScreen {
    isOnVSCodespaces: boolean;
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

export const SideBar: React.FunctionComponent<IRenderSplashScreenProps> = (props) => {
    return (
        <div className="sidebar">
            {props.isOnVSCodespaces
            ? <></>
            : (<svg className="logo" height="26" viewBox="0 0 16 16" version="1.1" width="26" aria-hidden="true">
                <path fillRule="evenodd" d="M8 0C3.58 0 0 3.58 0 8c0 3.54 2.29 6.53 5.47 7.59.4.07.55-.17.55-.38 0-.19-.01-.82-.01-1.49-2.01.37-2.53-.49-2.69-.94-.09-.23-.48-.94-.82-1.13-.28-.15-.68-.52-.01-.53.63-.01 1.08.58 1.23.82.72 1.21 1.87.87 2.33.66.07-.52.28-.87.51-1.07-1.78-.2-3.64-.89-3.64-3.95 0-.87.31-1.59.82-2.15-.08-.2-.36-1.02.08-2.12 0 0 .67-.21 2.2.82.64-.18 1.32-.27 2-.27.68 0 1.36.09 2 .27 1.53-1.04 2.2-.82 2.2-.82.44 1.1.16 1.92.08 2.12.51.56.82 1.27.82 2.15 0 3.07-1.87 3.75-3.65 3.95.29.25.54.73.54 1.48 0 1.07-.01 1.93-.01 2.2 0 .21.15.46.55.38A8.013 8.013 0 0016 8c0-4.42-3.58-8-8-8z"></path>
               </svg>)
            }
            
        </div>
    );
}

export const CodeLine: React.FunctionComponent<{percentage: number}> = (props) => {
    const style = {
        width: `${props.percentage}%`
    };
    return (
        <li className="l" style={style}></li>
    );
}

export const TreePane: React.FunctionComponent<{}> = () => {
    return (
        <div className="tree">
            <div className="l"></div>
                <ul className="lines">
                    <CodeLine percentage={60}/>
                    <CodeLine percentage={70}/>
                    <CodeLine percentage={50}/>
                    <CodeLine percentage={64}/>
                    <CodeLine percentage={83}/>
                    <CodeLine percentage={81}/>
                    <CodeLine percentage={40}/>
                    <CodeLine percentage={50}/>
                    <CodeLine percentage={68}/>
                    <CodeLine percentage={60}/>
                    <CodeLine percentage={75}/>
                    <CodeLine percentage={77}/>
                    <CodeLine percentage={40}/>
                </ul>
        </div>
    );
}

export const CodePane: React.FunctionComponent<{}> = () => {
    return (
        <div className="code">
            <ul className="lines">
                <CodeLine percentage={30}/>
                <CodeLine percentage={70}/>
                <CodeLine percentage={50}/>
                <CodeLine percentage={25}/>
                <CodeLine percentage={0}/>
                <CodeLine percentage={15}/>
                <CodeLine percentage={40}/>
                <CodeLine percentage={55}/>
                <CodeLine percentage={15}/>
                <CodeLine percentage={0}/>
                <CodeLine percentage={60}/>
                <CodeLine percentage={75}/>
                <CodeLine percentage={77}/>
                <CodeLine percentage={40}/>
            </ul>
        </div>
    );
}

export const CodePaneTabBar: React.FunctionComponent<{}> = () => {
    return (
        <ul className="tabs-code tabs">
            <li className="tab1">
                <div className="l"></div>
            </li>
            <li className="tab2">
                <div className="l"></div>
            </li>
        </ul>
    )
}

export const StepsPaneTabBar: React.FunctionComponent<{}> = () => {
    return (
        <ul className="tabs-steps tabs">
            <li className="tab1">
                <div className="l"></div>
            </li>
            <li className="tab2"></li>
        </ul>
    )
}

export const RenderSplashScreen: React.FunctionComponent<IRenderSplashScreenProps> = (props) => {
    const mainClass = classnames('container', {'is-vs-codespaces': props.isOnVSCodespaces});
    return (
        <div className={mainClass}>
            <SideBar
                isOnVSCodespaces={props.isOnVSCodespaces}/>
            <TreePane/>
            <CodePane/>
            <CodePaneTabBar/>
            <StepsPaneTabBar/>
            {props.children}
            <div className="bottom"></div>
        </div>
    );
};

export const ConnectSplashScreen: React.FunctionComponent<IConnectSplashScreen> = (props) => {
    return (
        <RenderSplashScreen isOnVSCodespaces={props.isOnVSCodespaces}>
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
                    <PrimaryButton onClick={props.onConnect}>Connect</PrimaryButton></Stack.Item>
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
    const connection = React.useMemo(() => { return new CommunicationProvider() }, []);
    const { showPrompt, environment, connectError, onRetry, onConnect } = props;
    const { friendlyName } = environment;
    const isOnVSCodespaces = !isHostedOnGithub();

    if (connectError !== null) {
        return (
            <ConnectSplashScreen
            isOnVSCodespaces={isOnVSCodespaces}
                title={`Connecting to environment ${friendlyName} failed. ${connectError}`}
                buttonText='Retry'
                onConnect={onRetry}
                >
            </ConnectSplashScreen>
        );
    }
    
    const envState = stateToDisplayName(environment!.state).toLocaleLowerCase();
    if (showPrompt) {
        const containerNaming = isOnVSCodespaces ? 'Environment' : 'Workspace';
        const title = `${containerNaming} "${friendlyName}" is ${envState}.`
        return (
            <ConnectSplashScreen
                isOnVSCodespaces={isOnVSCodespaces}
                title={title}
                buttonText='Connect'
                onConnect={onConnect}
                >
            </ConnectSplashScreen>
        );
    } else {
        return (
                <RenderSplashScreen isOnVSCodespaces={isOnVSCodespaces}>
                    <VSOSplashScreen connection={connection} github={!isOnVSCodespaces}></VSOSplashScreen>
                </RenderSplashScreen>
        );
    }

}