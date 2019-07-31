import React, { Component, Fragment } from 'react';
import { RouteComponentProps } from 'react-router';
import './workbench.css';

declare var AMDLoader: any;

AMDLoader.global.require.config({
    baseUrl: `${window.location.origin}/static/web-standalone/out`,
    paths: {
        'vscode-textmate': `${
            window.location.origin
        }/static/web-standalone/node_modules/vscode-textmate/release/main`,
        'onigasm-umd': `${
            window.location.origin
        }/static/web-standalone/node_modules/onigasm-umd/release/main`,
        xterm: `${window.location.origin}/static/web-standalone/node_modules/xterm/lib/xterm.js`,
        'xterm-addon-search': `${
            window.location.origin
        }/static/web-standalone/node_modules/xterm-addon-search/lib/xterm-addon-search.js`,
        'xterm-addon-web-links': `${
            window.location.origin
        }/static/web-standalone/node_modules/xterm-addon-web-links/lib/xterm-addon-web-links.js`,
        'semver-umd': `${
            window.location.origin
        }/static/web-standalone/node_modules/semver-umd/lib/semver-umd.js`,
    },
});

export interface WorkbenchProps extends RouteComponentProps {}

export interface WorkbenchState {
    isLoading?: boolean;
    friendlyName?: string;
}

let workbenchLoaded = false;

export class Workbench extends Component<WorkbenchProps, WorkbenchState> {
    componentDidMount() {
        AMDLoader.global.require(['vs/workbench/workbench.web.api'], (workbench: any) => {
            // 1. If we have authority setting, use it to configure the workbench
            let remoteAuthority: string = localStorage.getItem('vsonline.authority');
            let port: number = 8000; // development-time default
            if (remoteAuthority) {
                const [, authorityPort] = remoteAuthority.split(':');
                port = parseInt(authorityPort, 10) || port;
            } else {
                // 2. Try to use the authority based on port setting
                port = parseInt(localStorage.getItem('vsonline.port'), 10) || port;
                remoteAuthority = `localhost:${port}`;
            }

            // 3. Try to get workspace path
            const defaultDevPath = '/Users/pelisy/projects/learning/node/vscode-sample';
            const path = localStorage.getItem('vsonline.folderPath') || defaultDevPath;

            var config = {
                folderUri: {
                    $mid: 1,
                    path,
                    scheme: 'vscode-remote',
                    authority: remoteAuthority,
                },
                remoteAuthority,
                webviewEndpoint: `http://localhost:${port + 1}`,
            };

            if (workbenchLoaded) {
                return;
            }

            workbench.create(document.getElementById('workbench'), config);
            workbenchLoaded = true;
        });
    }

    componentWillUnmount() {
        workbenchLoaded = false;

        // tslint:disable-next-line: no-inner-html
        document.getElementById('workbench').innerHTML = '';
    }

    render() {
        return <Fragment />;
    }
}
