import React from 'react';
import ReactDOM from 'react-dom';
import { BrowserRouter } from 'react-router-dom';

import { App } from './app';
import { store } from './store/store';

import './index.css';
import { initializeIcons } from '@uifabric/icons';
import * as serviceWorker from './serviceWorker';

initializeIcons();

const baseUrl = (document.getElementById('public_url') as HTMLBaseElement).getAttribute('href');
const rootElement = document.getElementById('root');
if (process.env.NODE_ENV === 'development') {
    // localStorage.debug = 'vsa-portal-webapp,vsa-portal-webapp:*';
    localStorage.debug = 'static-assets-worker:*';
    // localStorage.debug = 'vs-ssh,vs-ssh:*';
} else {
    localStorage.debug = '';
}

ReactDOM.render(
    <BrowserRouter basename={baseUrl || ''}>
        <App store={store} />
    </BrowserRouter>,
    rootElement
);

// If you want your app to work offline and load faster, you can change
// unregister() to register() below. Note this comes with some pitfalls.
// Learn more about service workers: https://bit.ly/CRA-PWA
serviceWorker.register();
