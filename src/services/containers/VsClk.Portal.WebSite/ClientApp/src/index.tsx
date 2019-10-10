import React from 'react';
import ReactDOM from 'react-dom';
import { initializeIcons } from '@uifabric/icons';

import { App } from './app';
import { store } from './store/store';

import { postServiceWorkerMessage } from './common/post-message';
import * as serviceWorker from './serviceWorker';
import { configureServiceWorker } from './common/service-worker-messages';

import './index.css';
import { BrowserRouter } from 'react-router-dom';

function startApplication() {
    initializeIcons();

    const baseUrl = (document.getElementById('public_url') as HTMLBaseElement).getAttribute('href');
    const rootElement = document.getElementById('root');

    const enableTraceFactory = (traceName: string) => {
        return () => {
            localStorage.debug = traceName;
        };
    };

    const win = window as any;
    win.vsoEnablePortalTrace = enableTraceFactory('vsa-portal-webapp,vsa-portal-webapp:*');
    win.vsoEnableSshTrace = enableTraceFactory('vs-ssh,vs-ssh:*');

    ReactDOM.render(
        <BrowserRouter basename={baseUrl || ''}>
            <App store={store} />
        </BrowserRouter>,
        rootElement
    );

    // If you want your app to work offline and load faster, you can change
    // unregister() to register() below. Note this comes with some pitfalls.
    // Learn more about service workers: https://bit.ly/CRA-PWA
    serviceWorker.register({
        onReady() {
            postServiceWorkerMessage({
                type: configureServiceWorker,
                payload: {
                    features: {
                        useSharedConnection: true,
                    },
                },
            });
        },
    });
}

// Don't start application in iframe created by MSAL.
if (window.parent === window) {
    startApplication();
}
