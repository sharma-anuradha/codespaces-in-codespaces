import { isRecognizedGitUrl, normalizeGitUrl, getQueryableUrl } from '../gitUrlNormalization';

describe('git url utils', () => {
    describe('isRecognizedGitUrl', () => {
        test.each`
            url
            ${'https://github.com/vso/test'}
            ${'https://www.github.com/vso/test'}
            ${'https://github.com/vso/test.git'}
            ${'https://github.com/vso/test/'}
            ${'https://github.com/vso/test.git/'}
            ${'https://bitbucket.org/vso/test'}
            ${'https://vso@bitbucket.org/vso/test.git'}
            ${'https://gitlab.com/vso/test.git'}
        `('Valid url: "$url"', ({ url }) => {
            expect(isRecognizedGitUrl(url)).toBe(true);
        });

        test.each`
            repositoryName
            ${'vso/test'}
            ${'VSO/test'}
            ${'VSO/1.test'}
            ${'VSO/_test'}
            ${'VSO/-test'}
            ${'VSO/1test_'}
            ${'VSO/_test1'}
            ${'VSO/TEST1'}
            ${'VSO1/test'}
            ${'VSO-1/test'}
            ${'VSO--1/test'}
            ${'VSO---1/test'}
        `('Valid GitHub repository names: "$repositoryName"', ({ repositoryName }) => {
            expect(isRecognizedGitUrl(repositoryName)).toBe(true);
        });

        test.each`
            url
            ${'https://github.com/vso/test/pull/18'}
        `('Valid pull request url: "$url"', ({ url }) => {
            expect(isRecognizedGitUrl(url)).toBe(true);
        });

        test.each`
            url
            ${'https://github.com/vso/test/tree/dev'}
            ${'https://github.com/vso/test/tree/V1.0'}
            ${'https://github.com/vso/test/commit/0abd362eb99c696a74fec56a766ee1910c7598e1'}
        `('Valid tree and commit url: "$url"', ({ url }) => {
            expect(isRecognizedGitUrl(url)).toBe(true);
        });

        test.each`
            url
            ${''}
            ${'   '}
            ${'42'}
            ${'hello'}
            ${'-Vso/test'}
            ${'VSO-/test'}
            ${'🍺/test'}
            ${'VSO/test#'}
            ${'VSO/test🍻'}
            ${'VSO_1/test'}
            ${'_VSO1/test'}
            ${'_/test'}
            ${'https://some-random-service.com/vso/test.git/'}
            ${'https://github.com/'}
            ${'https://github.com/1'}
            ${'https://github.com/test'}
            ${'https://github.com/_test'}
            ${'https://bitbucket.org/'}
            ${'https://bitbucket.org/1'}
            ${'https://bitbucket.org/test'}
            ${'https://bitbucket.org/_test'}
            ${'https://gitlab.com/'}
            ${'https://gitlab.com/1'}
            ${'https://gitlab.com/test'}
            ${'https://gitlab.com/_test'}
        `('Invalid url: "$url"', ({ url }) => {
            expect(isRecognizedGitUrl(url)).toBe(false);
        });
    });

    describe('normalizeRepositoryUrl', () => {
        test.each`
            url
            ${'https://github.com/vso/test.git'}
            ${'https://github.com/vso/1.test.git'}
            ${'https://vso@bitbucket.org/vso/test.git'}
            ${'https://gitlab.com/vso/test.git'}
            ${'https://gitlab.com/vso/test/'}
        `('leaves valid git url (as provided by the clone dialogs) as is: "$url"', ({ url }) => {
            expect(normalizeGitUrl(url)).toBe(url);
        });

        test.each`
            url
            ${'https://gitlab.com/vso/test/'}
            ${'https://github.com/vso/test.git/'}
        `('leaves valid git url as is: "$url"', ({ url }) => {
            expect(normalizeGitUrl(url)).toBe(url);
        });

        test.each`
            url
            ${'https://github.com/vso/test/pull/18'}
        `('leaves pull request "$url" as is', ({ url }) => {
            expect(normalizeGitUrl(url)).toBe(url);
        });

        test.each`
            shortHandRepositoryName | fullRepositoryUrl
            ${'vso/test'}           | ${'https://github.com/vso/test'}
            ${'vso/1.test'}         | ${'https://github.com/vso/1.test'}
            ${'microsoft/vscode'}   | ${'https://github.com/microsoft/vscode'}
        `(
            'transforms "$shortHandRepositoryName" into "$fullRepositoryUrl"',
            ({ shortHandRepositoryName, fullRepositoryUrl }) => {
                expect(normalizeGitUrl(shortHandRepositoryName)).toBe(fullRepositoryUrl);
            }
        );

        test.each`
            repositoryName
            ${'42'}
            ${'   '}
            ${'🌈 & 🦄'}
            ${'@yoda/thinking'}
            ${'https://some-random-service.com/vso/test.git'}
        `('returns undefined for invalid "$repositoryName"', ({ repositoryName }) => {
            expect(normalizeGitUrl(repositoryName)).toBeUndefined();
        });
    });

    describe('getQueryableUrl', () => {
        test.each`
            original                                    | queryable
            ${'https://github.com/vso/test.git'}        | ${'https://api.github.com/repos/vso/test'}
            ${'https://github.com/vso/test'}            | ${'https://api.github.com/repos/vso/test'}
            ${'vso/test'}                               | ${'https://api.github.com/repos/vso/test'}
            ${'microsoft/vscode'}                       | ${'https://api.github.com/repos/microsoft/vscode'}
            ${'https://vso@bitbucket.org/vso/test.git'} | ${'https://api.bitbucket.org/2.0/repositories/vso/test'}
            ${'https://gitlab.com/vso/test.git'}        | ${'https://gitlab.com/api/v4/projects/vso%2Ftest'}
        `('transforms "$original" into "$queryable"', ({ original, queryable }) => {
            expect(getQueryableUrl(original)).toBe(queryable);
        });

        test.each`
            url                                      | repositoryApiUrl
            ${'https://github.com/vso/test/pull/18'} | ${'https://api.github.com/repos/vso/test'}
        `('transforms pull request "$url" to "$repositoryApiUrl"', ({ url, repositoryApiUrl }) => {
            expect(getQueryableUrl(url)).toBe(repositoryApiUrl);
        });

        test.each`
            repositoryName
            ${'42'}
            ${'   '}
            ${'🌈 & 🦄'}
            ${'@yoda/thinking'}
            ${'https://some-random-service.com/vso/test.git'}
        `('returns undefined for invalid "$repositoryName"', ({ repositoryName }) => {
            expect(getQueryableUrl(repositoryName)).toBeUndefined();
        });
    });
});
