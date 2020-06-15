import { GitCredentialsRequest } from '../interfaces/GitCredentialsRequest';

export function parseGitCredentialsFillInput(str: string): GitCredentialsRequest {
    // Git asks for credentials in form of string request
    // E.g:
    //      protocol=https
    //      host=github.com
    //
    // https://git-scm.com/docs/git-credential#IOFMT
    //
    const lines = str.split('\n');
    let result: GitCredentialsRequest = {};
    return lines.reduce((parsedInput: any, line) => {
        const [key, value] = getKeyValuePair(line);

        if (key) {
            parsedInput[key] = value;
        }

        return parsedInput;
    }, result);

    function getKeyValuePair(line: string) {
        const delimiterIndex = line.indexOf('=');
        if (delimiterIndex <= 0) {
            return [];
        }
        return [line.slice(0, delimiterIndex), line.slice(delimiterIndex + 1)];
    }
}
