import React, { Component } from 'react';
import './serverlessWorkbench.css';

import { vscode } from '../../utils/vscode';
import { IWorkbenchConstructionOptions, URI, IURLCallbackProvider } from 'vscode-web';

import { credentialsProvider } from '../../providers/credentialsProvider';
import { UrlCallbackProvider } from '../../providers/urlCallbackProvider';
import { UserDataProvider } from '../../utils/userDataProvider';

import { telemetry, sendTelemetry } from '../../utils/telemetry';
import * as path from 'path';
import { trace } from '../../utils/trace';
import { FolderWorkspaceProvider } from '../../providers/folderWorkspaceProvider';

export interface ServerlessWorkbenchProps {
    folderUri: string;
    staticExtensions?: { packageJSON: any; extensionLocation: string }[];
    extensionUrls?: string[];
    resolveExternalUri?: (uri: URI) => Promise<URI>;
    urlCallbackProvider?: IURLCallbackProvider;
    targetURLFactory?: (folderUri: URI) => URL | undefined;
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

type StaticExtension = { packageJSON: any; extensionLocation: URI };
function isNotNullStaticExtension(se: StaticExtension | undefined): se is StaticExtension {
    return !!se;
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

        const packagesWithMainThatWork = ['vscode.python', 'ms-vscode.references-view'];

        const packages = keys.map((modulePath: string) => {
            const packageJSON = context(modulePath);
            const extensionName = `${packageJSON.publisher}.${packageJSON.name}`;

            if (packageJSON.main && !packagesWithMainThatWork.includes(extensionName)) {
                return undefined; // unsupported
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

        return packages.filter(isNotNullStaticExtension);
    }

    private async getExtensionFromUrl(extensionLocation: string) {
        try {
            const packageJsonPath = `${extensionLocation}/package.json`;
            const response = await fetch(packageJsonPath, { method: 'GET' });
            if (!response.ok) {
                sendTelemetry('vsonline/extensionload/error', new Error(response.statusText));
                return undefined;
            }

            const packageJSON = await response.json();
            return {
                packageJSON,
                extensionLocation: vscode.URI.parse(extensionLocation),
            };
        } catch (error) {
            sendTelemetry('vsonline/extensionload/error', error);
        }
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

        const workspaceProvider = new FolderWorkspaceProvider(
            this.props.folderUri,
            this.props.targetURLFactory
        );

        const resolveExternalUri = this.props.resolveExternalUri;

        let staticExtensions: StaticExtension[] = [];

        if (this.props.extensionUrls) {
            const extensionsFromUrls = (
                await Promise.all(
                    this.props.extensionUrls.map(async (url) => await this.getExtensionFromUrl(url))
                )
            ).filter(isNotNullStaticExtension);

            staticExtensions = staticExtensions.concat(extensionsFromUrls);
        }

        if (this.props.staticExtensions) {
            staticExtensions = staticExtensions.concat(
                this.props.staticExtensions.map((se) => {
                    return {
                        packageJSON: se.packageJSON,
                        extensionLocation: vscode.URI.parse(se.extensionLocation),
                    };
                })
            );
        }

        staticExtensions = staticExtensions.concat(this.getBuiltinStaticExtensions());

        const { urlCallbackProvider = new UrlCallbackProvider() } = this.props;

        const config: IWorkbenchConstructionOptions = {
            workspaceProvider,
            urlCallbackProvider,
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
