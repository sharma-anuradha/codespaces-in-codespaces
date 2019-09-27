import { URI } from 'vscode-web';

import { vscodeConfig } from '../constants';
import { EnvConnector } from '../ts-agent/envConnector';
import { vscode } from '../utils/vscode';

type VSCodeServerConnectionDetails = {
    readonly connectionToken: string;
};

type AssetRoutingDetails = {
    readonly containerUrl: URL;
    readonly originalUrl: URL;
};

type ContainerRoutingDetails = {
    readonly containerUrl: URL;
    readonly originalUrl: URL;
};

type BaseRoutingDetails = {
    readonly port: number;
    readonly sessionId: string;
    readonly vscodeConnectionDetails?: VSCodeServerConnectionDetails;
};

export type RoutingDetails = BaseRoutingDetails & (AssetRoutingDetails | ContainerRoutingDetails);

const assetsPathComponent = 'assets';
const vscodeRemoteResourcePathComponent = 'vscode-remote-resource';
/**
 * We send requests over the LiveShare connection to localhost:80 and then agent routes the traffic to the right
 * port based on channel we send it through.
 */
const containerDefaultHost = 'localhost:80';

export function getRoutingDetails(url: string): Readonly<RoutingDetails> | undefined {
    // As a part of embedding VSCode, we transform asset URLs into a form that we can easily
    // parse and it contains necessary information to route requests properly.
    // The requests will have form of:
    //
    //      https://<host>/assets/:sessionId/:port/vscode-remote-resource?path=<urlEncodedPathToAsset>&tkn=<connectionToken>
    //
    // For example:
    //
    //      https://online.visualstudio.com/vscode-remote-resource?path=%2Fhome%2Fcloudenv%2F.vscode-remote%2Fextensions%2Fms-vsliveshare.vsliveshare-1.0.809%2Fimages%2Fdark%2Fliveshare.svg&tkn=b7a5b7cf39f569a570599530ffc855b454e37b06
    //
    const originalUrl = new URL(url);

    return (
        tryCreatePortForwardingRoutingDetails(originalUrl) ||
        tryCreateVSCodeAssetRoutingDetails(originalUrl)
    );
}

function tryCreatePortForwardingRoutingDetails(
    originalUrl: URL
): Readonly<RoutingDetails> | undefined {
    const [targetSubdomain, app] = originalUrl.host.split('.');

    // Port forwarding domains are in form of:
    // https://<sessionId>-<targetPort>.app.online.visualstudio.com/<user content path>
    if (app !== 'app') {
        return undefined;
    }

    const [maybeSessionId, maybePort, ...rest] = targetSubdomain.split('-');

    // If there's more data in the target subdomain, it's not us.
    if (rest && rest.length > 0) {
        return undefined;
    }

    if (!maybeSessionId) {
        return undefined;
    }

    const port = Number(maybePort);
    if (!isValidPort(port)) {
        return undefined;
    }

    const containerUrl = new URL(originalUrl.href);
    containerUrl.host = containerDefaultHost;

    return {
        sessionId: maybeSessionId,
        port,
        originalUrl,
        containerUrl,
    };
}

export function tryCreateVSCodeAssetRoutingDetails(
    originalUrl: URL
): Readonly<RoutingDetails> | undefined {
    const containerUrl = new URL(originalUrl.href);
    containerUrl.host = containerDefaultHost;

    let port: number;

    // Pathname from URL starts with '/'.
    const [
        maybeAssetsPathComponent,
        maybeSessionId,
        maybePort,
        maybeVSCodeRemoteResourcePathComponent,
    ] = originalUrl.pathname.substr(1).split('/');

    const alwaysSendToContainer =
        maybeAssetsPathComponent === assetsPathComponent &&
        maybeVSCodeRemoteResourcePathComponent === vscodeRemoteResourcePathComponent;

    if (!alwaysSendToContainer) {
        return undefined;
    }

    const sessionId = maybeSessionId;

    port = Number(maybePort);
    if (!isValidPort(port)) {
        return undefined;
    }

    const path = originalUrl.searchParams.get('path');
    if (path) {
        containerUrl.pathname = `${vscodeRemoteResourcePathComponent}`;
    }
    let vscodeConnectionDetails: VSCodeServerConnectionDetails | undefined;

    const tkn = originalUrl.searchParams.get('tkn');
    if (tkn) {
        vscodeConnectionDetails = {
            connectionToken: tkn,
        };
    }

    return {
        sessionId,
        port,
        originalUrl,
        containerUrl,
        vscodeConnectionDetails,
    };
}

export function isValidPort(port: number): boolean {
    return !isNaN(port) && port >= 0 && port <= 65535;
}

export function resourceUriProviderFactory(sessionId: string, connector: EnvConnector) {
    let portNumber: number | undefined = undefined;

    // The connection might get restarted and with that we might be forced to spin up a new server.
    connector.onVSCodeServerStarted(({ port }) => {
        portNumber = port;
    });

    return (uri: URI): URI => {
        if (!portNumber) {
            throw new Error(
                'Cannot resolve asset URLs before connection to cloud environment is established.'
            );
        }

        return vscode.URI.from({
            scheme: 'https',
            authority: window.location.host,
            // We attach vscodeRemoteResourcePathComponent at the end for easier recognizability when compared with self host.
            path: `/${assetsPathComponent}/${sessionId}/${portNumber}/${vscodeRemoteResourcePathComponent}`,
            query: `path=${encodeURIComponent(uri.path)}&tkn=${vscodeConfig.commit}`,
        });
    };
}
