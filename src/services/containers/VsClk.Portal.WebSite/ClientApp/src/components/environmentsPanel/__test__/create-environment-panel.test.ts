import { test_setMockRequestFactory, createMockMakeRequestFactory } from '../../../utils/testUtils';

import { validateGitRepository, validationMessages } from '../create-environment-panel';
jest.mock('../../../utils/telemetry');

describe('create-environment-panel', () => {
    describe('validateGitRepository', () => {
        let responseStatus = 200;
        beforeEach(() => {
            test_setMockRequestFactory(
                createMockMakeRequestFactory({
                    responses: [
                        {
                            get status() {
                                return responseStatus;
                            },
                        },
                    ],
                })
            );
        });

        afterEach(() => {
            responseStatus = 200;
        });

        it('allows empty repository', async () => {
            expect(await validateGitRepository('', null)).toBe(validationMessages.valid);
        });

        it('fails empty repository when required', async () => {
            expect(await validateGitRepository('', null, null, true)).toBe(
                validationMessages.invalidGitUrl
            );
        });

        it('fails with repo we cannot clone', async () => {
            responseStatus = 404;
            expect(
                await validateGitRepository(
                    'https://some-random-service.com/vso/test.git',
                    null,
                    null,
                    true
                )
            ).toBe(validationMessages.invalidGitUrl);
        });

        it('allows existing public GitHub repository name', async () => {
            responseStatus = 200;
            expect(await validateGitRepository('vso/public', null, null, true)).toBe(
                validationMessages.valid
            );
        });

        it('fails non-existent public or private GitHub repository name', async () => {
            responseStatus = 404;
            expect(await validateGitRepository('vso/public', null, null, true)).toBe(
                validationMessages.privateRepoNoAuth
            );
        });

        it('allows existing private GitHub repository name with git credentials', async () => {
            responseStatus = 200;
            expect(await validateGitRepository('vso/private', 'github_access_token', null, true)).toBe(
                validationMessages.valid
            );
        });

        it('allows existing private GitHub repository name with github and azdev credentials', async () => {
            responseStatus = 200;
            expect(await validateGitRepository('vso/private', 'github_access_token', 'azdev_access_token', true)).toBe(
                validationMessages.valid
            );
        });

        it('allows BitBucket repository', async () => {
            responseStatus = 200;
            expect(await validateGitRepository('https://vso@bitbucket.org/vso/test.git')).toBe(
                validationMessages.valid
            );
        });

        it('fails BitBucket repository', async () => {
            responseStatus = 404;
            expect(await validateGitRepository('https://vso@bitbucket.org/vso/test.git')).toBe(
                validationMessages.unableToConnect
            );
        });

        it('allows GitLab repository', async () => {
            responseStatus = 200;
            expect(await validateGitRepository('https://gitlab.com/vso/test.git')).toBe(
                validationMessages.valid
            );
        });

        it('fails GitLab repository', async () => {
            responseStatus = 404;
            expect(await validateGitRepository('https://gitlab.com/vso/test.git')).toBe(
                validationMessages.unableToConnect
            );
        });

        it('allows AzureDevOps repository', async () => {
            responseStatus = 200;
            expect(await validateGitRepository('https://dev.azure.com/devdiv/OnlineServices/_git/vsclk-core', null, 'azdev_access_token', true)).toBe(
                validationMessages.valid
            );
        });

        it('allows AzureDevOps repository with GitHub and AzDev tokens', async () => {
            responseStatus = 200;
            expect(await validateGitRepository('https://dev.azure.com/devdiv/OnlineServices/_git/vsclk-core', 'github_access_token', 'azdev_access_token', true)).toBe(
                validationMessages.valid
            );
        });

        it('fails AzureDevOps repository', async () => {
            responseStatus = 404;
            expect(await validateGitRepository('https://dev.azure.com/devdiv/OnlineServices/_git/vsclk-core', null, null, true)).toBe(
                validationMessages.privateRepoNoAuth
            );
        });

        it('fails AzureDevOps repository with github token', async () => {
            responseStatus = 404;
            expect(await validateGitRepository('https://dev.azure.com/devdiv/OnlineServices/_git/vsclk-core', 'github_access_token', null, true)).toBe(
                validationMessages.privateRepoNoAuth
            );
        });
    });
});
