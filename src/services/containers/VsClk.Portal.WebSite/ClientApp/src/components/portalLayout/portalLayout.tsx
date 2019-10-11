import React from 'react';

import { TitleBar } from '../titlebar/titlebar';
import { Navigation } from '../navigation/navigation';

import './portalLayout.css';

export function PortalLayout({
    children,
    hideNavigation,
}: React.PropsWithChildren<{ hideNavigation?: boolean }>) {
    const navigation = hideNavigation ? null : (
        <div className='ms-Grid-col ms-bgColor-gray20 portalLayout__app-navigation-container'>
            <Navigation />
        </div>
    );
    return (
        <div className='ms-Grid portalLayout'>
            <div className='ms-Grid-row'>
                <TitleBar />
            </div>
            <div className='ms-Grid-row portalLayout__app-content'>
                {navigation}
                <div className='ms-Grid-col portalLayout__app-content-container'>{children}</div>
            </div>
        </div>
    );
}
