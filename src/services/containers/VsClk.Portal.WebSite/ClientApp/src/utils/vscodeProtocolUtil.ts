import { ILocalEnvironment, EnvironmentStateInfo } from 'vso-client-core';

import protocolCheck from 'custom-protocol-check';
import { connectEnvironment } from '../services/envRegService';
import { createUniqueId } from '../dependencies';

//tslint:disable-next-line: export-name
export async function tryOpeningUrl(environment: ILocalEnvironment, protocol: 'vscode' | 'vscode-insiders') {
	if (environment.state === EnvironmentStateInfo.Shutdown) {
		await connectEnvironment(environment.id!, environment.state);
	}

	const url = `ms-vsonline.vsonline/connect?environmentId=${encodeURIComponent(
		environment.id!
	)}&sessionPath=${
		environment.connection!.sessionPath
	}&correlationId=${createUniqueId()}`;

	await checkProtocol(`${protocol}://${url}`);
}

export async function checkProtocol(url: string){
	return new Promise((resolve, reject) => {
        protocolCheck(url, 
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