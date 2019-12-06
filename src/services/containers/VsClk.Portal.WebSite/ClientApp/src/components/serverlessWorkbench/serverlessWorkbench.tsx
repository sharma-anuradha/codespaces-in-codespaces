import React, { Component } from 'react';
import './serverlessWorkbench.css';

import { vscode } from '../../utils/vscode';
import { IWorkbenchConstructionOptions, IWorkspaceProvider, URI } from 'vscode-web';

import { createUniqueId } from '../../dependencies';
import { credentialsProvider } from '../../providers/credentialsProvider';
import { UrlCallbackProvider } from '../../providers/urlCallbackProvider';
import { UserDataProvider } from '../../utils/userDataProvider';

import { telemetry } from '../../utils/telemetry';
import * as path from 'path';
import { trace } from '../../utils/trace';

export interface ServerlessWorkbenchProps {
    folderUri: string;
    staticExtensions: { packageJSON: any; extensionLocation: string }[];
    resolveExternalUri?: (uri: URI) => Promise<URI>;
}

const managementFavicon = 'favicon.ico';
const vscodeFavicon = 'static/web-standalone/favicon.ico';
function updateFavicon(isMounting: boolean = true) {
    const link = document.querySelector("link[rel='shortcut icon']");
    if (link) {
        const iconPath = isMounting ? vscodeFavicon : managementFavicon;
        link.setAttribute('href', iconPath);
    }
}

export class ServerlessWorkbench extends Component<
    ServerlessWorkbenchProps,
    ServerlessWorkbenchProps
> {
    // Since we have external scripts running outside of react scope,
    // we'll mange the instantiation flag outside of state as well.
    private workbenchMounted: boolean = false;

    constructor(props: ServerlessWorkbenchProps) {
        super(props);
    }

    componentDidMount() {
        updateFavicon(true);
        this.mountWorkbench();
    }

    componentWillUnmount() {
        updateFavicon(false);
    }

    private getBuiltinStaticExtensions() {
        // Webpack parses require.context and makes those files available in the bundle. So all the package.json
        // files in the extensions dir will be in the bundle and the .keys() property will be populated at build time
        // to be all the relative paths for the package.json. So, keys will be like './csharp/package.json'
        const context = require.context('extensions', true, /^\.\/[^\/]*\/package.json$/);
        const keys = context.keys();
        const packages = keys.map((modulePath: string) => {
            const packageJSON = context(modulePath);
            if (packageJSON.main) {
                return undefined; // unsupported
            }

            if (packageJSON.name === 'scss') {
                return undefined; // seems to fail to JSON.parse()?!
            }

            packageJSON.extensionKind = ['web'];
            const packageDirName = path.basename(path.dirname(modulePath));
            return {
                packageJSON,
                extensionLocation: vscode.URI.parse(
                    `https://${window.location.hostname}/static/web-standalone/server/stable/extensions/${packageDirName}/`
                ),
            };
        });

        type StaticExtension = { packageJSON: any; extensionLocation: URI };
        return packages.filter((p: StaticExtension | undefined): p is StaticExtension => !!p);
    }

    async mountWorkbench() {
        if (this.workbenchMounted) {
            return;
        }

        await vscode.getVSCode();

        if (!this.workbenchRef) {
            return;
        }

        this.workbenchMounted = true;

        const userDataProvider = new UserDataProvider();
        await userDataProvider.initializeDBProvider();

        const resolveCommonTelemetryProperties = telemetry.resolveCommonProperties.bind(telemetry);

        const workspaceProvider: IWorkspaceProvider = {
            workspace: {
                folderUri: vscode.URI.parse(this.props.folderUri),
            },
            // Opening workspaces from this view is not supported.
            open: async () => {},
        };

        const resolveExternalUri = this.props.resolveExternalUri;
        const staticExtensions = this.props.staticExtensions
            .map((se) => {
                return {
                    packageJSON: se.packageJSON,
                    extensionLocation: vscode.URI.parse(se.extensionLocation),
                };
            })
            .concat(this.getBuiltinStaticExtensions());

        const quality =
            window.localStorage.getItem('vso-featureset') === 'insider' ? 'insider' : 'stable';

        const config: IWorkbenchConstructionOptions = {
            workspaceProvider,
            urlCallbackProvider: new UrlCallbackProvider(quality),
            credentialsProvider,
            userDataProvider,
            resolveExternalUri,
            resolveCommonTelemetryProperties,
            staticExtensions,
        };

        trace(`Creating workbench on #${this.workbenchRef}, with config: `, config);
        vscode.create(this.workbenchRef, config);
    }

    private workbenchRef: HTMLDivElement | null = null;

    render() {
        return (
            <div className='vsonline-workbench'>
                <div
                    id='workbench'
                    style={{ height: '100%' }}
                    ref={
                        // tslint:disable-next-line: react-this-binding-issue
                        (el) => (this.workbenchRef = el)
                    }
                />
            </div>
        );
    }
}
