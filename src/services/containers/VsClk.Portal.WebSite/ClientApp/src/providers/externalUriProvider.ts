import { isHostedOnGithub, IEnvironment } from 'vso-client-core';

import { EnvConnector } from 'vso-ts-agent';

import { vscode, BaseExternalUriProvider as WorkbenchBaseExternalUriProvider } from 'vso-workbench';

import { getPFDomain, getCurrentEnvironment } from '../utils/getPortForwardingDomain';

import { URI } from 'vscode-web';
import { sendTelemetry } from '../utils/telemetry';
import { setAuthCookie } from '../utils/setAuthCookie';
import { getAuthTokenAction } from '../actions/getAuthTokenActionCommon';

abstract class BaseExternalUriProvider extends WorkbenchBaseExternalUriProvider {
    public async resolveExternalUri(uri: URI): Promise<URI> {
        const port = this.getLocalHostPortToForward(uri);
        if (port === undefined) {
            return uri;
        }

        sendTelemetry('vsonline/portal/resolve-external-uri', { port });
        await this.ensurePortIsForwarded(port);

        let authority: string;
        if (isHostedOnGithub()) {
            const environment = getCurrentEnvironment();
            const pfDomain = getPFDomain(environment);
            authority = `${this.sessionId}-${port}.${pfDomain}`;
        } else {
            authority = `${this.sessionId}-${port}.app.${window.location.hostname}`;
        }

        const newUri = new vscode.URI('https', authority, uri.path, uri.query, uri.fragment);

        //set cookie to authenticate PortForwarding
        const getAuthToken = getAuthTokenAction();
        const token = await getAuthToken();
        if (token === undefined) {
            throw new Error('No token available.');
        }
        await setAuthCookie(token, `${newUri.scheme}://${newUri.authority}`);

        return newUri;
    }
}

export class EnvironmentsExternalUriProvider extends BaseExternalUriProvider {
    protected async ensurePortIsForwarded(port: number): Promise<void> {
        await this.connector.ensurePortIsForwarded(
            this.environmentInfo,
            this.accessToken,
            this.liveShareEndpoint,
            port
        );
    }

    constructor(
        private readonly environmentInfo: IEnvironment,
        private readonly accessToken: string,
        private readonly connector: EnvConnector,
        private readonly liveShareEndpoint: string
    ) {
        super(environmentInfo.connection.sessionId);
    }
}

export class LiveShareExternalUriProvider extends BaseExternalUriProvider {
    protected async ensurePortIsForwarded(port: number): Promise<void> {
        // Nothing to do since the port is already forwarded by the LiveShare host.
    }

    constructor(sessionId: string) {
        super(sessionId);
    }
}

// TODO: clean up when we have a good way to reuse telemetry.
export class PortForwardingExternalUriProvider {
    private static readonly allowedSchemes = ['http', 'https'];
    private static readonly allowedHosts = ['localhost', '127.0.0.1', '0.0.0.0'];

    constructor(
        private readonly portForwardingDomainTemplate: string,
        private readonly id: string,
        private readonly ensurePortForwarded: (port: number) => Promise<void> = async () => {}
    ) {
        this.resolveExternalUri = this.resolveExternalUri.bind(this);
        this.getPortFromUri = this.getPortFromUri.bind(this);
    }

    async resolveExternalUri(uri: URI): Promise<URI> {
        if (!PortForwardingExternalUriProvider.allowedSchemes.includes(uri.scheme)) {
            return uri;
        }

        let port = this.getPortFromUri(uri);

        if (!port) {
            return uri;
        }

        this.ensurePortForwarded(port);

        sendTelemetry('vsonline/portal/resolve-external-uri', { port });

        const authority = this.portForwardingDomainTemplate.replace(
            '{0}',
            `${this.id.toLowerCase()}-${port}`
        );

        return new vscode.URI('https', authority, uri.path, uri.query, uri.fragment);
    }

    getPortFromUri(uri: URI): number | null {
        try {
            const url = new URL(`${uri.scheme}://${uri.authority}`);

            if (!PortForwardingExternalUriProvider.allowedHosts.includes(url.hostname)) {
                return null;
            }

            let portString = url.port;

            if (!portString) {
                switch (uri.scheme) {
                    case 'http':
                        portString = '80';
                        break;
                    case 'https':
                        portString = '443';
                        break;
                }
            }

            if (!/^\d{2,5}$/.test(portString)) {
                return null;
            }

            return +portString;
        } catch {
            return null;
        }
    }
}
