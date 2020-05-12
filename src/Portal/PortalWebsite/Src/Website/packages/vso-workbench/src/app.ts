import * as React from 'react';
import * as ReactDom from 'react-dom';

import { telemetry } from './telemetry/telemetry';
import App from './react-app/app';

const el = document.querySelector('#js-vscode-workbench');
telemetry.initializeTelemetry((_: string) => null);

if (!el) {
    throw new Error('No workbench DOM element found.');
}

ReactDom.render(React.createElement(App), el);
