const fs = require('fs');

const vscodeRepo = './vscode-remote';

// A few temporary patches to the vscode-remote repo so we can build it here
removeOptionalDependencies(`${vscodeRepo}/remote/package.json`);
setTypingsRoot(`${vscodeRepo}/build/tsconfig.build.json`);
setTypingsRoot(`${vscodeRepo}/test/smoke/tsconfig.json`);
fixMocksFile(`${vscodeRepo}/extensions/open-ssh-remote/src/test/mocks.ts`);

function removeOptionalDependencies(file) {
    const content = JSON.parse(fs.readFileSync(file));
    delete content["optionalDependencies"];
    fs.writeFileSync(file, JSON.stringify(content, null, 2));
}

function setTypingsRoot(file) {
    const content = JSON.parse(fs.readFileSync(file));
    content["compilerOptions"].typeRoots = ["./node_modules/@types"];
    fs.writeFileSync(file, JSON.stringify(content, null, "\t"));
}

function fixMocksFile(file) {
    fs.writeFileSync(file,
`import { ILogger } from "../utils/logger";

/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

export class MockLogger implements ILogger {
	info(message: string) { }

	trace(message: string) { }

	debug(message: string) { }

	error(message: string) { }

	dispose() { }

	showOutputChannel() { }
}
`)
}
