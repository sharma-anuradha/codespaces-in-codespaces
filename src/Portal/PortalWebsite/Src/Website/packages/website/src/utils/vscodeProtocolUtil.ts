import { ILocalEnvironment, EnvironmentStateInfo, IEnvironment } from 'vso-client-core';

import protocolCheck from 'custom-protocol-check';
import { connectEnvironment } from '../services/envRegService';
import { createUniqueId } from '../dependencies';

//tslint:disable-next-line: export-name
export async function tryOpeningUrl(
    environment: ILocalEnvironment,
    protocol: 'vscode' | 'vscode-insiders'
) {
    if (environment.state === EnvironmentStateInfo.Shutdown) {
        await connectEnvironment(environment as IEnvironment);
    }

    const { id, connection } = environment;

    if (!id) {
        throw new Error('No environment "id" set.');
    }

    if (!connection) {
        throw new Error('No environment "connection" set.');
    }

    const url = `ms-vsonline.vsonline/connect?environmentId=${encodeURIComponent(id)}&sessionPath=${
        connection.sessionPath
    }&correlationId=${createUniqueId()}&authScheme=aad`;

    await checkProtocol(`${protocol}://${url}`);
}

export async function checkProtocol(url: string) {
    return new Promise((resolve, reject) => {
        protocolCheck(
            url,
            () => {
                reject(new Error('Failed to open url:' + url));
            },
            () => resolve(),
            () => {
                reject(new Error('Failed unexpectedly: ' + url));
            }
        );
    });
}
