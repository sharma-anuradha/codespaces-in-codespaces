import React, { Component } from 'react';
import { RouteComponentProps } from 'react-router-dom';

import './main.css';

import { TitleBar } from '../titlebar/titlebar';
import { Navigation } from '../navigation/navigation';

import { EnvironmentsPanel } from '../environmentsPanel/environments-panel';
import { amdConfig } from '../../amd/amdConfig';

export class Main extends Component {
    componentDidMount() {
        this.initializeWorkbenchFetching();
    }

    private initializeWorkbenchFetching() {
        if (amdConfig()) {
            AMDLoader.global.require(['vs/workbench/workbench.web.api'], (_: any) => {});
        }
    }

    render() {
        return (
            <div className='ms-Grid main'>
                <div className='ms-Grid-row'>
                    <TitleBar />
                </div>
                <div className='ms-Grid-row main__app-content'>
                    <div className='ms-Grid-col ms-bgColor-gray20 main__app-navigation-container'>
                        <Navigation />
                    </div>
                    <div className='ms-Grid-col main__app-content-container'>
                        <EnvironmentsPanel />
                    </div>
                </div>
            </div>
        );
    }
}
