import { test_setMockRequestFactory, createMockMakeRequestFactory } from '../../../utils/testUtils';

import { validateGitRepository, getValidationMessage } from '../create-environment-panel';
const englishStrings = require("../../../loc/resources/WebsiteStringResources.json");

jest.mock('../../../utils/telemetry');

const mockTranslationFunc = (key: string) => {
    if (!key) {
        return undefined;
    }
    return englishStrings[key];
}

describe('create-environment-panel', () => {
    describe('validateGitRepository', () => {
        let responseStatus = 200;
        let vsoFeatureSet = window.localStorage.getItem('vso-featureset');
        beforeEach(() => {
            window.localStorage.setItem('vso-featureset', 'insider');
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
            if (vsoFeatureSet) {
                window.localStorage.setItem('vso-featureset', vsoFeatureSet);
            } else {
                window.localStorage.removeItem('vso-featureset');
            }
        });

        it('allows empty repository', async () => {
            const message = await validateGitRepository('', null);
            expect(getValidationMessage(message, mockTranslationFunc)).toBe(englishStrings['valid']);
        });

        it('fails empty repository when required', async () => {
            const message = await validateGitRepository('', null, null, true);
            expect(getValidationMessage(message, mockTranslationFunc)).toBe(
                englishStrings.invalidGitUrl
            );
        });

        it('fails with repo we cannot clone', async () => {
            responseStatus = 404;
            const message = await validateGitRepository(
                'https://some-random-service.com/vso/test.git',
                null,
                null,
                true
            );
            expect(getValidationMessage(message, mockTranslationFunc)).toBe(englishStrings.invalidGitUrl);
        });

        it('allows existing public GitHub repository name', async () => {
            responseStatus = 200;
            const message = await validateGitRepository('vso/public', null, null, true);
            expect(getValidationMessage(message, mockTranslationFunc)).toBe(
                englishStrings.valid
            );
        });

        it('fails non-existent public or private GitHub repository name', async () => {
            responseStatus = 404;
            const message = await validateGitRepository('vso/public', null, null, true);
            expect(getValidationMessage(message, mockTranslationFunc)).toBe(
                englishStrings.privateRepoNoAuth
            );
        });

        it('allows existing private GitHub repository name with git credentials', async () => {
            responseStatus = 200;
            const message = await validateGitRepository('vso/private', 'github_access_token', null, true);
            expect(getValidationMessage(message, mockTranslationFunc)).toBe(
                englishStrings.valid
            );
        });

        it('allows existing private GitHub repository name with github and azdev credentials', async () => {
            responseStatus = 200;
            const message = await validateGitRepository('vso/private', 'github_access_token', 'azdev_access_token', true);
            expect(getValidationMessage(message, mockTranslationFunc)).toBe(
                englishStrings.valid
            );
        });

        it('allows BitBucket repository', async () => {
            responseStatus = 200;
            const message = await validateGitRepository('https://vso@bitbucket.org/vso/test.git');
            expect(getValidationMessage(message, mockTranslationFunc)).toBe(
                englishStrings.valid
            );
        });

        it('fails BitBucket repository', async () => {
            responseStatus = 404;
            const message = await validateGitRepository('https://vso@bitbucket.org/vso/test.git')
            expect(getValidationMessage(message, mockTranslationFunc)).toBe(
                englishStrings.unableToConnect
            );
        });

        it('allows GitLab repository', async () => {
            responseStatus = 200;
            const message = await validateGitRepository('https://gitlab.com/vso/test.git');
            expect(getValidationMessage(message, mockTranslationFunc)).toBe(
                englishStrings.valid
            );
        });

        it('fails GitLab repository', async () => {
            responseStatus = 404;
            const message = await validateGitRepository('https://gitlab.com/vso/test.git');
            expect(getValidationMessage(message, mockTranslationFunc)).toBe(
                englishStrings.unableToConnect
            );
        });

        it('allows AzureDevOps repository', async () => {
            responseStatus = 200;
            const message = await validateGitRepository('https://dev.azure.com/devdiv/OnlineServices/_git/vsclk-core', null, 'azdev_access_token', true);
            expect(getValidationMessage(message, mockTranslationFunc)).toBe(
                englishStrings.valid
            );
        });

        it('allows AzureDevOps repository with GitHub and AzDev tokens', async () => {
            responseStatus = 200;
            const message = await validateGitRepository('https://dev.azure.com/devdiv/OnlineServices/_git/vsclk-core', 'github_access_token', 'azdev_access_token', true);
            expect(getValidationMessage(message, mockTranslationFunc)).toBe(
                englishStrings.valid
            );
        });

        it('fails AzureDevOps repository', async () => {
            responseStatus = 404;
            const message = await validateGitRepository('https://dev.azure.com/devdiv/OnlineServices/_git/vsclk-core', null, null, true);
            expect(getValidationMessage(message, mockTranslationFunc)).toBe(
                englishStrings.privateRepoNoAuth
            );
        });

        it('fails AzureDevOps repository with github token', async () => {
            responseStatus = 404;
            const message = await validateGitRepository('https://dev.azure.com/devdiv/OnlineServices/_git/vsclk-core', 'github_access_token', null, true);
            expect(getValidationMessage(message, mockTranslationFunc)).toBe(
                englishStrings.privateRepoNoAuth
            );
        });
    });
});
