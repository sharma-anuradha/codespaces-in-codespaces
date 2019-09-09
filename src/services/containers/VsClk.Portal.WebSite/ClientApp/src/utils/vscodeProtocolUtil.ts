import protocolCheck from 'custom-protocol-check';

//tslint:disable-next-line: export-name
export async function tryOpeningUrl(url: string) {
    return new Promise((resolve, reject) => {
        protocolCheck(url, 
            () => {
				// tslint:disable-next-line: no-suspicious-comment
				/* Hack: 
					If VSCode is not installed, we need to reload from cache
					to be able to verify if vscode-insiders is installed.
					This is only needed for Chrome browser.
				*/
				if (url.toString().includes('vscode://')) {
					window.location.reload(false);
				}
                reject(new Error('Failed to open url:' + url));
            }, 
            () => resolve(), 
            () => {
				// tslint:disable-next-line: no-suspicious-comment
				/* Hack: 
					If VSCode is not installed, we need to reload from cache
					to be able to verify if vscode-insiders is installed.
					This is only needed for Chrome browser.
				*/
				if (url.toString().includes('vscode://')) {
					window.location.reload(false);
				}
				reject(new Error('Failed unexpectedly: ' + url));
			}
        );
    });
}