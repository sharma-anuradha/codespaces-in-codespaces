import React, { SFC } from 'react';

import { isHostedOnGithub } from 'vso-client-core';

import { TitleBar } from '../titlebar/titlebar';
import { Navigation } from '../navigation/navigation';

import './portalLayout.css';
import { RouteComponentProps, withRouter } from 'react-router-dom';

const MaybeNavigation = (props: { hideNavigation?: boolean }) => {
    if (props.hideNavigation) {
        return null;
    }

    return (
        <div className='ms-Grid-col ms-bgColor-gray20 portal-layout__app-navigation-container'>
            <Navigation />
        </div>
    );
};

const MaybeHeader: SFC<RouteComponentProps<any>> = (props) => {
    if (isHostedOnGithub()) {
        return null;
    }

    return (
        <header className='ms-Grid-row'>
            <TitleBar {...props} />
        </header>
    );
};

export const PortalLayout: React.ComponentClass<React.PropsWithChildren<{
    hideNavigation?: boolean;
}>> = withRouter(
    ({
        children,
        hideNavigation,
        ...rest
    }: React.PropsWithChildren<{ hideNavigation?: boolean }> & RouteComponentProps) => {
        return (
            <div className='ms-Grid portal-layout'>
                <MaybeHeader {...rest} />
                <main className='ms-Grid-row portal-layout__app-content'>
                    <MaybeNavigation hideNavigation={hideNavigation} />
                    <div className='ms-Grid-col portal-layout__app-content-container'>
                        {children}
                    </div>
                </main>
            </div>
        );
    }
);
