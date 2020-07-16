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
    const { isOnVSCodespaces } = props;

    // currently only GitHub but depending on branding discussions,
    // we might expose the icon in the public API
    const mainClass = !isOnVSCodespaces ? 'is-logo' : '';
    // `is-vs-codespaces` is the legacy class name that should be deprecated in favor or `is-dark-theme`
    // currently used in the `vsonline-splash-screen` package
    const containerClass = isOnVSCodespaces ? 'is-dark-theme is-vs-codespaces' : '';

    return (
        <div className={`vso-splash-screen ${mainClass}`}>
            <div className={`container ${containerClass}`}>
                <SideBar />
                <TreePane />
                <CodePane />
                <StepsPane> {props.children} </StepsPane>
                <CodePaneTabBar />
                <StepsPaneTabBar />
                <div className='bottom'></div>
            </div>
        </div>
    );
};
