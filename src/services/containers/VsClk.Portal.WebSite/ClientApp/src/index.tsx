import React from 'react';
import ReactDOM from 'react-dom';
import { BrowserRouter } from 'react-router-dom';
import { initializeIcons } from '@uifabric/icons';

import { App } from './app';
import { store } from './store/store';
import { VSLS_API_URI } from './constants';

import { postServiceWorkerMessage } from './common/post-message';
import * as serviceWorker from './serviceWorker';
import { configureServiceWorker } from './common/service-worker-messages';

import './index.css';

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
win.vsoEnableStaticAssetsSWTrace = enableTraceFactory('service-worker:*');

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
                liveShareEndpoint: VSLS_API_URI,
                features: {
                    useSharedConnection: true,
                },
            },
        });
    },
});
