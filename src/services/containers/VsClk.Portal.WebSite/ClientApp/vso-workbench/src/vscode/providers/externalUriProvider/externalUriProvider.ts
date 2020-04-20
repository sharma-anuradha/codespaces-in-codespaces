import { URI } from 'vscode-web';
import { vscode } from '../../vscodeAssets/vscode';

import { IEnvironment } from 'vso-client-core';
import { EnvConnector } from 'vso-ts-agent';

import { authService } from '../../../auth/authService';
import { setAuthCookie } from '../../../auth/portForwarding/setAuthCookie';
import { sendTelemetry } from '../../../telemetry/sendTelemetry';
import { AuthenticationError } from '../../../errors/AuthenticationError';

export abstract class BaseExternalUriProvider {
    protected abstract ensurePortIsForwarded(port: number): Promise<void>;

    constructor(protected readonly sessionId: string) { }

    public async resolveExternalUri(uri: URI): Promise<URI> {
        const port = this.getLocalHostPortToForward(uri);
        if (port === undefined) {
            return uri;
        }

        sendTelemetry('vsonline/workbench/resolve-external-uri', { port });
        await this.ensurePortIsForwarded(port);

        const pfDomain = location.origin.split('//').pop();
        const newUri = new vscode.URI('https', `${this.sessionId}-${port}.${pfDomain}`, uri.path, uri.query);

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
            port,
            this.liveShareEndpoint
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
