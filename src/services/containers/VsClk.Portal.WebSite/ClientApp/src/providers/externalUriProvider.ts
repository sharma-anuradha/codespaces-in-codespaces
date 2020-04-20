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
            const pfDomain = getPFDomain(environment)
                .split('//')
                .pop();
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
