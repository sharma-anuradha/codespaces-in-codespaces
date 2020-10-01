import {
    GITHUB_DOT_DEV_TLD,
    GITHUB_LOCAL_TLD,
    IEnvironment,
} from 'vso-client-core';
import { TEnvironment } from '../config/config';

export const isValidGithubSubdomain = (envInfo: IEnvironment, env: TEnvironment) => {
    const { friendlyName } = envInfo;

    switch (env) {
        case 'production': {
            return location.hostname === `${friendlyName}.${GITHUB_DOT_DEV_TLD}`;
        }

        case 'staging': {
            return location.hostname === `${friendlyName}.ppe.${GITHUB_DOT_DEV_TLD}`;

        }

        case 'development': {
            return location.hostname === `${friendlyName}.dev.${GITHUB_DOT_DEV_TLD}`;
        }

        case 'local': {
            return location.hostname === `codespace-${friendlyName}.${GITHUB_LOCAL_TLD}`;
        }
    }
};
