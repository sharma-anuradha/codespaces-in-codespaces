import { isHostedOnGithub, IEnvironment } from 'vso-client-core';

import { EnvConnector } from 'vso-ts-agent';

import { BaseExternalUriProvider as WorkbenchBaseExternalUriProvider } from 'vso-workbench';

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

        uri.scheme = 'https';
        if (isHostedOnGithub()) {
            const environment = getCurrentEnvironment();
            const pfDomain = getPFDomain(environment)
                .split('//')
                .pop();
            uri.authority = `${this.sessionId}-${port}.${pfDomain}`;
        } else {
            uri.authority = `${this.sessionId}-${port}.app.${window.location.hostname}`;
        }

        //set cookie to authenticate PortForwarding
        const getAuthToken = getAuthTokenAction();
        const token = await getAuthToken();
        if (token === undefined) {
            throw new Error('No token available.');
        }
        await setAuthCookie(token, `${uri.scheme}://${uri.authority}`);

        return uri;
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
