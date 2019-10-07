import { URI } from 'vscode-web';
import { ICloudEnvironment } from '../interfaces/cloudenvironment';
import { EnvConnector } from '../ts-agent/envConnector';

export class ExternalUriProvider {
    constructor(
        private readonly environmentInfo: ICloudEnvironment,
        private readonly accessToken: string,
        private readonly connector: EnvConnector,
        private readonly liveShareEndpoint: string
    ) {}

    public async resolveExternalUri(uri: URI): Promise<URI> {
        const port = this.getLocalHostPortToForward(uri);
        if (port === undefined) {
            return uri;
        }

        await this.connector.ensurePortIsForwarded(
            this.environmentInfo,
            this.accessToken,
            port,
            this.liveShareEndpoint
        );

        uri.authority = `${this.environmentInfo.connection.sessionId}-${port}.app.${window.location.hostname}`;
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
