import React from 'react';
import ReactDOM from 'react-dom';
import { BrowserRouter } from 'react-router-dom';
import Modal from 'react-modal';
import './index.css';
import { App } from './app';
import * as serviceWorker from './serviceWorker';

import { initializeIcons } from '@uifabric/icons';
initializeIcons();

const baseUrl = (document.getElementById('public_url') as HTMLBaseElement).getAttribute('href');
const rootElement = document.getElementById('root');

// localStorage.debug = 'vsa-portal-webapp,vsa-portal-webapp:*';
// localStorage.debug = 'vs-ssh,vs-ssh:*';
localStorage.debug = '';

Modal.setAppElement('body');

ReactDOM.render(
    <BrowserRouter basename={baseUrl || ''}>
        <App key='main-app' />
    </BrowserRouter>, rootElement
);

// If you want your app to work offline and load faster, you can change
// unregister() to register() below. Note this comes with some pitfalls.
// Learn more about service workers: https://bit.ly/CRA-PWA
serviceWorker.unregister();
