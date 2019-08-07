import React from 'react';
import ReactDOM from 'react-dom';
import { BrowserRouter } from 'react-router-dom';
import Modal from 'react-modal';
import './index.css';
import { App } from './app';
import * as serviceWorker from './serviceWorker';

import { configureStore } from './store/configureStore';

import { initializeIcons } from '@uifabric/icons';

// We are not sure if we want to go with the theme yet.
// import { loadTheme } from 'office-ui-fabric-react/lib/Styling';
// import { theme } from './theme/theme';

// loadTheme(theme);

initializeIcons();

const baseUrl = (document.getElementById('public_url') as HTMLBaseElement).getAttribute('href');
const rootElement = document.getElementById('root');
if (process.env.NODE_ENV === 'development') {
    localStorage.debug = 'vsa-portal-webapp,vsa-portal-webapp:*';
    // localStorage.debug = 'vs-ssh,vs-ssh:*';
} else {
    localStorage.debug = '';
}

Modal.setAppElement('body');

const store = configureStore();

ReactDOM.render(
    <BrowserRouter basename={baseUrl || ''}>
        <App store={store} />
    </BrowserRouter>,
    rootElement
);

// If you want your app to work offline and load faster, you can change
// unregister() to register() below. Note this comes with some pitfalls.
// Learn more about service workers: https://bit.ly/CRA-PWA
serviceWorker.unregister();
