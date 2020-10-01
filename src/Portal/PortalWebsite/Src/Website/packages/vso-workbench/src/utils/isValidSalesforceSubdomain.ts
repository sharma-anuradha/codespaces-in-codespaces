import {
    IEnvironment, SALESFORCE_TLD,
} from 'vso-client-core';

import { TEnvironment } from '../config/config';

export const isValidSalesforceSubdomain = (envInfo: IEnvironment, env: TEnvironment) => {
    const { id } = envInfo;

    switch (env) {
        case 'production': {
            return location.hostname === `${id}.${SALESFORCE_TLD}`;
        }

        case 'staging': {
            return location.hostname === `${id}.ppe.${SALESFORCE_TLD}`;
        }

        case 'development': {
            return location.hostname === `${id}.dev.${SALESFORCE_TLD}`;
        }

        case 'local': {
            return location.hostname === `${id}.local.${SALESFORCE_TLD}`;
        }
    }
};
