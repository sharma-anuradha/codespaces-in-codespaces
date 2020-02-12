import { URI } from 'vscode-web';
import { EnvConnector } from '../ts-agent/envConnector';
import { vscode } from '../utils/vscode';
import { assetsPathComponent, vscodeRemoteResourcePathComponent } from '../common/url-utils';

export function resourceUriProviderFactory(
    connectionToken: string,
    sessionId: string,
    connector: EnvConnector
) {
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
        const query = new URLSearchParams();
        query.set('path', uri.path);
        query.set('tkn', connectionToken);
        return vscode.URI.from({
            scheme: 'https',
            authority: window.location.host,
            // We attach vscodeRemoteResourcePathComponent at the end for easier recognizability when compared with self host.
            path: `/${assetsPathComponent}/${sessionId}/${portNumber}/${vscodeRemoteResourcePathComponent}`,
            query: query.toString(),
        });
    };
}
