/**
 * Deprecated for the workbench page but still used by the old workbench page in the VSCS portal.
 */

import { isHostedOnGithub } from 'vso-client-core';
import {
    DEFAULT_EXTENSIONS,
    DEFAULT_NON_ESSENTIAL_EXTENSIONS,
    HOSTED_IN_GITHUB_EXTENSIONS
} from '../../constants';

export const getFirstRunExtensions = (): string[] => {
    const defaultExtensions = [...DEFAULT_EXTENSIONS];

    if (!isHostedOnGithub()) {
        return [...defaultExtensions];
    }

    return [
        ...defaultExtensions,
        ...HOSTED_IN_GITHUB_EXTENSIONS,
    ];
};

export const getSecondRunExtensions = (): string[] => {
    if (!isHostedOnGithub()) {
        return [...DEFAULT_EXTENSIONS];
    }

    return [
        ...DEFAULT_EXTENSIONS,
        ...HOSTED_IN_GITHUB_EXTENSIONS,
    ];
};

export const getExtensions = (isFirstRun: boolean): string[] => {
    // TODO: move the extensions into the platform info payload instead
    if (isFirstRun) {
        return getFirstRunExtensions();
    }

    return getSecondRunExtensions();
};
