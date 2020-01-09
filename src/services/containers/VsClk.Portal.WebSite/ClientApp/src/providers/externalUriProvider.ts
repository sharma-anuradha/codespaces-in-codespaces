import { URI } from 'vscode-web';
import { ICloudEnvironment } from '../interfaces/cloudenvironment';
import { EnvConnector } from '../ts-agent/envConnector';
import { sendTelemetry } from '../utils/telemetry';

abstract class BaseExternalUriProvider {
    protected abstract ensurePortIsForwarded(port: number): Promise<void>;

    constructor(private readonly sessionId: string) {}

    public async resolveExternalUri(uri: URI): Promise<URI> {
        const port = this.getLocalHostPortToForward(uri);
        if (port === undefined) {
            return uri;
        }

        sendTelemetry('vsonline/portal/resolve-external-uri', { port });
        await this.ensurePortIsForwarded(port);

        uri.scheme = 'https';
        uri.authority = `${this.sessionId}-${port}.app.${window.location.hostname}`;
        return uri;
    }

    private getLocalHostPortToForward(uri: URI): number | undefined {
        if (uri.scheme !== 'http' && uri.scheme !== 'https') {
            return undefined;
        }
        const localhostMatch = /^(localhost|127\.0\.0\.1|0\.0\.0\.0):(\d+)$/.exec(uri.authority);
        if (!localhostMatch) {
            return undefined;
        }
        return +localhostMatch[2];
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
        private readonly environmentInfo: ICloudEnvironment,
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
