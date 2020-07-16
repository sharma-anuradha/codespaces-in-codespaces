import { createTrace, maybePii } from 'vso-client-core';

import {
    GitCredentialService as GitCredentialServiceBase,
    parseGitCredentialsFillInput,
} from 'vso-ts-agent';
import { findGitCredential } from '../vscode/providers/credentialsProvider/strategies/GitServiceCredentialsStrategy';

export const trace = createTrace('GitCredentialService');

export class GitCredentialService extends GitCredentialServiceBase {
    public async onRequest([input]: string[]) {
        const fillRequest = parseGitCredentialsFillInput(input);

        const { host, path = '/', protocol = '' } = fillRequest;

        if (!protocol.startsWith('http') || !host || !path) {
            trace.verbose('Not enough request attributes to respond', maybePii(fillRequest));
            return input;
        }

        const result = await findGitCredential(host, path);
        if (result) {
            trace.verbose('Fulfilled git credentials request', maybePii(fillRequest));
            return result;
        }

        trace.verbose('Failed to fill credential.', maybePii(fillRequest));

        return input;
    }
}
