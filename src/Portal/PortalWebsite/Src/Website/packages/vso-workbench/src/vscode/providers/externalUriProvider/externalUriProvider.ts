import { URI } from 'vscode-web';
import { IEnvironment } from 'vso-client-core';

import { vscode } from '../../vscodeAssets/vscode';
import { EnvConnector } from '../../../clients/envConnector';
import { authService } from '../../../auth/authService';
import { setAuthCookie } from '../../../auth/portForwarding/setAuthCookie';
import { sendTelemetry } from '../../../telemetry/sendTelemetry';
import { AuthenticationError } from '../../../errors/AuthenticationError';

export abstract class BaseExternalUriProvider {
    protected abstract ensurePortIsForwarded(port: number): Promise<void>;

    constructor(protected readonly sessionId: string) {}

    public async resolveExternalUri(uri: URI): Promise<URI> {
        const port = this.getLocalHostPortToForward(uri);
        if (port === undefined) {
            return uri;
        }

        sendTelemetry('vsonline/workbench/resolve-external-uri', { port });
        await this.ensurePortIsForwarded(port);

        const pfDomain = location.origin.split('//').pop();
        const newUri = new vscode.URI(
            'https',
            `${this.sessionId}-${port}.${pfDomain}`,
            uri.path,
            uri.query,
            uri.fragment
        );

        // set cookie to authenticate PortForwarding
        const token = await authService.getCachedToken();
        if (!token) {
            throw new AuthenticationError('No token available.');
        }

        await setAuthCookie(token, `${newUri.scheme}://${newUri.authority}`);

        return newUri;
    }

    protected getLocalHostPortToForward(uri: URI): number | undefined {
        const defaultHttpPort = 80;
        const defaultHttpsPort = 443;
        if (uri.scheme !== 'http' && uri.scheme !== 'https') {
            return undefined;
        }
        const localhostMatch = /^(localhost|127\.0\.0\.1|0\.0\.0\.0)(:\d+)?$/.exec(uri.authority);
        if (!localhostMatch) {
            return undefined;
        }

        if (localhostMatch == undefined) {
            return uri.scheme == 'http' ? defaultHttpPort : defaultHttpsPort;
        } else {
            return +localhostMatch[2].substr(1, localhostMatch[2].length);
        }
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
        private readonly liveShareEndpoint: string,
    ) {
        super(environmentInfo.connection.sessionId);
    }
}

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

        sendTelemetry('vsonline/workbench/resolve-external-uri', { port });

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