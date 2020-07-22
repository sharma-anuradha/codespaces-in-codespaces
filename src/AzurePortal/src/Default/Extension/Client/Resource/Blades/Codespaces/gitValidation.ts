import * as Validations from 'Fx/Controls/Validations';
import { normalizeGitUrl, getQueryableUrl } from './gitUrlNormalization';
import { getSupportedGitService, SupportedGitService } from './getSupportedGitService';
import { HttpClient } from '../../../Shared/HttpClient';

export enum validationMessagesKeys {
    valid = 'valid',
    noAccess = 'noAccess',
    testFailed = 'testFailed',
    nameIsRequired = 'nameIsRequired',
    nameIsTooLong = 'nameIsTooLong',
    nameIsInvalid = 'nameIsInvalid',
    unableToConnect = 'unableToConnect',
    invalidGitUrl = 'invalidGitUrl',
    noAccessDotFiles = 'noAccessDotFiles',
    privateRepoNoAuth = 'privateRepoNoAuth',
    noPlanSelected = 'noPlanSelected',
    noSkusAvailable = 'noSkusAvailable',
}

export function validateGitUrl(
    maybeRepository: string,
    gitHubAccessToken: string | null = null,
    azDevAccessToken: string | null = null,
    required = false
): Q.Promise<validationMessagesKeys> {
    const maybeGitUrl = normalizeGitUrl(maybeRepository);
    if (!required && !maybeRepository) {
        return Q(validationMessagesKeys.valid);
    }
    if (!maybeGitUrl) {
        return Q(validationMessagesKeys.invalidGitUrl);
    }
    const gitServiceProvider = getSupportedGitService(maybeGitUrl);
    const queryableUrl = getQueryableUrl(maybeGitUrl);
    if (!queryableUrl) {
        return Q(validationMessagesKeys.invalidGitUrl);
    }
    if (gitServiceProvider === SupportedGitService.GitHub && gitHubAccessToken) {
        return queryGitService(queryableUrl, gitHubAccessToken)
            .then((isAccessible) => {
                if (!isAccessible) {
                    return validationMessagesKeys.noAccess;
                } else {
                    return validationMessagesKeys.valid;
                }
            })
            .catch(() => {
                return validationMessagesKeys.noAccess;
            });
    } else if (gitServiceProvider === SupportedGitService.GitHub) {
        return queryGitService(queryableUrl)
            .then((isAccessible) => {
                if (!isAccessible) {
                    return validationMessagesKeys.privateRepoNoAuth;
                } else {
                    return validationMessagesKeys.valid;
                }
            })
            .catch(() => {
                return validationMessagesKeys.testFailed;
            });
    } else if (gitServiceProvider === SupportedGitService.AzureDevOps && azDevAccessToken) {
        // ToDo: Check to see if AzDevOpsRepo is a valid Repo
        return Q(validationMessagesKeys.valid);
    } else if (gitServiceProvider === SupportedGitService.AzureDevOps) {
        // ToDo: Check if AzureDevOps repo is a public repo. https://docs.microsoft.com/en-us/azure/devops/organizations/public/make-project-public?view=azure-devops
        return Q(validationMessagesKeys.privateRepoNoAuth);
    } else {
        return queryGitService(queryableUrl)
            .then((isAccessible) => {
                if (isAccessible) {
                    return validationMessagesKeys.valid;
                } else {
                    return validationMessagesKeys.unableToConnect;
                }
            })
            .catch(() => {
                return validationMessagesKeys.testFailed;
            });
    }
}

export function getValidationMessage(
    key: validationMessagesKeys,
    translationFunc: (key: string) => string
): Q.Promise<Validations.ValidationResult> {
    return Q({
        valid: key === validationMessagesKeys.valid,
        message: translationFunc(key.toString()),
    });
}

function queryGitService(url: string, bearerToken?: string): Q.Promise<boolean> {
    const headers: Record<string, string> = {
        'Content-Type': 'application/json',
    };
    if (bearerToken) {
        headers['Authorization'] = `Bearer ${bearerToken}`;
    }
    return Q(new HttpClient()).then((client) =>
        client.get<any>(url.toString(), {
            headers: headers,
            success: function () {
                return true;
            },
            error: function () {
                //TODO: return false or throw based on error cause
                return false;
            },
        })
    );
}
