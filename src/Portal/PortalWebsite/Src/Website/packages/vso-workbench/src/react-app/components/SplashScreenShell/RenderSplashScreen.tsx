import React from 'react';

import { SideBar } from './SideBar';
import { IRenderSplashScreenProps } from './IRenderSplashScreenProps';
import { TreePane } from './TreePane';
import { CodePane } from './CodePane';
import { CodePaneTabBar } from './CodePaneTabBar';
import { StepsPaneTabBar } from './StepsPaneTabBar';
import { StepsPane } from './StepsPane';

import './SplashScreenShell.css';

export const RenderSplashScreen: React.FunctionComponent<IRenderSplashScreenProps> = (props) => {
    const mainClass = (props.isOnVSCodespaces)
        ? 'is-vs-codespaces'
        : '';
    
    return (<div className='vso-splash-screen'>
        <div className={`container ${mainClass}`}>
            <SideBar isOnVSCodespaces={props.isOnVSCodespaces} />
            <TreePane />
            <CodePane />
            <StepsPane> {props.children} </StepsPane>
            <CodePaneTabBar />
            <StepsPaneTabBar />
            <div className="bottom"></div>
        </div>
    </div>);
};
