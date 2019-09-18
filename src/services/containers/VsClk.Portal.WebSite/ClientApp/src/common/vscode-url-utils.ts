import { URI } from 'vscode-web';

import { vscodeConfig } from '../constants';
import { EnvConnector } from '../ts-agent/envConnector';
import { vscode } from '../utils/vscode';

export type ParsedAssetRequestUrl = {
    readonly isAssetUrl: true;
    readonly hash: string;
    readonly host: string;
    readonly href: string;
    /**
     * Warning: Contains trailing `:`
     */
    readonly protocol: string;
    readonly sessionId: string;
    /**
     * VSCode server has an endpoint for serving files form disk:
     *
     *      vscodeServerEndpoint
     *
     * For example:
     *
     *      https://localhost:8000/vscode-remote-resource?path=%2Fhome%2Fcloudenv%2F.cloudenv-settings.json&tkn=b7a5b7...
     *
     * TODO: #982891 - Implement static assets server with more limited access than what VSCode server has.
     */
    readonly vscodeServerEndpoint: string;
    readonly port: number;
    readonly path: string;
    readonly tkn: string;
};

export type BasicParsedUrl = URL & {
    readonly isAssetUrl: false;
};

export type ParsedUrl = ParsedAssetRequestUrl | BasicParsedUrl;

const assetsPathComponent = 'assets';
const vscodeRemoteResourcePathComponent = 'vscode-remote-resource';

export function parseRequestUrl(url: string): ParsedUrl {
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

    const parsedUrl = new URL(url);

    // Pathname from URL starts with '/'.
    const [
        maybeAssetsPathComponent,
        sessionId,
        maybePort,
        maybeVSCodeRemoteResourcePathComponent,
    ] = parsedUrl.pathname.substr(1).split('/');

    if (
        maybeAssetsPathComponent !== assetsPathComponent &&
        maybeVSCodeRemoteResourcePathComponent !== vscodeRemoteResourcePathComponent
    ) {
        // If assets and vscode-remote-resource aren't in their respective places,
        // it's not one of ours.
        return { ...parsedUrl, isAssetUrl: false };
    }

    const port = Number(maybePort);
    if (isNaN(port) || port < 0 || port >= 65535) {
        return { ...parsedUrl, isAssetUrl: false };
    }

    const path = parsedUrl.searchParams.get('path');
    if (!path) {
        return { ...parsedUrl, isAssetUrl: false };
    }

    const tkn = parsedUrl.searchParams.get('tkn');
    if (!tkn) {
        return { ...parsedUrl, isAssetUrl: false };
    }

    // We'll send everything after path with the request
    const everythingAfterPath = parsedUrl.href.substr(
        parsedUrl.origin.length + parsedUrl.pathname.length
    );

    return {
        isAssetUrl: true,
        hash: parsedUrl.hash,
        host: parsedUrl.host,
        href: parsedUrl.href,
        protocol: parsedUrl.protocol,
        vscodeServerEndpoint: `/${vscodeRemoteResourcePathComponent}${everythingAfterPath}`,
        sessionId,
        port,
        path,
        tkn,
    };
}

export function resourceUriProviderFactory(sessionId: string, connector: EnvConnector) {

    const connectionParams: { port: number | undefined } = {
        port: undefined,
    };

    // The connection might get restarted and with that we might be forced to spin up a new server.
    connector.onVSCodeServerStarted(({ port }) => {
        connectionParams.port = port;
    });

    return (uri: URI): URI => {
        if (!connectionParams.port) {
            throw new Error(
                'Cannot resolve asset URLs before connection to cloud environment is established.'
            );
        }

        return vscode.URI.from({
            scheme: 'https',
            authority: window.location.host,
            // We attach vscodeRemoteResourcePathComponent at the end for easier recognizability when compared with self host.
            path: `/${assetsPathComponent}/${sessionId}/${connectionParams.port}/${vscodeRemoteResourcePathComponent}`,
            query: `path=${encodeURIComponent(uri.path)}&tkn=${vscodeConfig.commit}`,
        });
    };
}
