import * as path from 'path';

import React, { Component } from 'react';

import {
    UserDataProvider,
    vscode,
    getVSCodeAssetPath,
    UrlCallbackProvider,
    getVSCodeVersionString,
    getVSCodeVersion,
} from 'vso-workbench';

import { IWorkbenchConstructionOptions, URI, IURLCallbackProvider, IHostCommand } from 'vscode-web';

import { credentialsProvider } from '../../providers/credentialsProvider';

import { telemetry, sendTelemetry } from '../../utils/telemetry';
import { trace } from '../../utils/trace';
import { FolderWorkspaceProvider } from '../../providers/folderWorkspaceProvider';
import { updateFavicon } from '../../utils/updateFavicon';

import './serverlessWorkbench.css';

export enum RepoType_QueryParam {
    GitHub = 'GitHub',
    AzureDevOps = 'AzureDevOps',
}

export interface ServerlessWorkbenchProps {
    folderUri: string;
    staticExtensions?: { packageJSON: any; extensionLocation: string }[];
    extensionUrls?: string[];
    resolveExternalUri?: (uri: URI) => Promise<URI>;
    urlCallbackProvider?: IURLCallbackProvider;
    targetURLFactory?: (folderUri: URI) => URL | undefined;
    resolveCommands?: () => Promise<IHostCommand[]>;
}

const managementFavicon = 'favicon.ico';
const vscodeFavicon = getVSCodeAssetPath(managementFavicon);

type StaticExtension = { packageJSON: any; extensionLocation: URI };
function isNotNullStaticExtension(se: StaticExtension | undefined): se is StaticExtension {
    return !!se;
}

export class ServerlessWorkbench extends Component<ServerlessWorkbenchProps> {
    // Since we have external scripts running outside of react scope,
    // we'll mange the instantiation flag outside of state as well.
    private workbenchMounted: boolean = false;

    componentDidMount() {
        updateFavicon(vscodeFavicon);
        this.mountWorkbench();
    }

    componentWillUnmount() {
        updateFavicon(managementFavicon);
    }

    private getBuiltinStaticExtensions() {
        const version = getVSCodeVersion();

        // VSCode packages extension with the web drop but that's not in stable yet. So keep using the old way of using
        // extensions from the server drop until stable web drop has the
        if (version.quality === 'stable') {
            // Webpack parses require.context and makes those files available in the bundle. So all the package.json
            // files in the extensions dir will be in the bundle and the .keys() property will be populated at build time
            // to be all the relative paths for the package.json. So, keys will be like './csharp/package.json'
            const context = (require as any).context(
                'web-standalone',
                true,
                /^\.\/extensions\/(insider|stable)\-[a-f0-9]{7}\/[^\/]*\/package.json$/
            );
            const keys = context
                .keys()
                .filter((key: string) =>
                    key.startsWith(`./extensions/${getVSCodeVersionString()}`)
                );
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
                        `https://${
                            window.location.hostname
                        }/workbench-page/web-standalone/extensions/${getVSCodeVersionString()}/${packageDirName}/`
                    ),
                };
            });

            return packages.filter(isNotNullStaticExtension);
        }

        // Webpack parses require.context and makes those files available in the bundle. So all the package.json
        // files in the extensions dir will be in the bundle and the .keys() property will be populated at build time
        // to be all the relative paths for the package.json. So, keys will be like './csharp/package.json'
        const context = (require as any).context(
            'web-standalone',
            true,
            /^\.\/(insider|stable)\-[a-f0-9]{7}\/extensions\/[^\/]*\/package.json$/
        );
        const keys = context
            .keys()
            .filter((key: string) => key.startsWith(`./${getVSCodeVersionString()}`));

        const packages = keys.map((modulePath: string) => {
            const packageJSON = context(modulePath);
            const packageDirName = path.basename(path.dirname(modulePath));
            return {
                packageJSON,
                extensionLocation: vscode.URI.parse(
                    `https://${
                        window.location.hostname
                    }/workbench-page/web-standalone/${getVSCodeVersionString()}/extensions/${packageDirName}/`
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

        const userDataProvider = new UserDataProvider(async () => {
            return '';
        });

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

        const { urlCallbackProvider = new UrlCallbackProvider(), resolveCommands } = this.props;
        const commands = resolveCommands ? await resolveCommands() : undefined;
        const config: IWorkbenchConstructionOptions = {
            workspaceProvider,
            urlCallbackProvider,
            credentialsProvider,
            userDataProvider,
            resolveExternalUri,
            resolveCommonTelemetryProperties,
            staticExtensions,
            commands,
        };

        trace(`Creating workbench on #${this.workbenchRef}, with config: `, config);
        vscode.create(this.workbenchRef, config);
    }

    private workbenchRef: HTMLDivElement | null = null;

    render() {
        return (
            <div className='vsonline-serverless-workbench'>
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
