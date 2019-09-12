import React, { Component } from 'react';
import { RouteComponentProps } from 'react-router-dom';

import './main.css';

import { TitleBar } from '../titlebar/titlebar';
import { Navigation } from '../navigation/navigation';

import { EnvironmentsPanel } from '../environmentsPanel/environments-panel';

export class Main extends Component {
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
