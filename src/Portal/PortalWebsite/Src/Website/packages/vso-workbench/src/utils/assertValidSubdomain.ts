import { getParentDomain, IEnvironment } from "vso-client-core";
import { config } from '../config/config';
import { SubdomainMismatchError } from "../errors/SubdomainMismatchError";
import { sendTelemetry } from "../telemetry/telemetry";
import { isValidCodespaceSubdomain } from './isValidCodespaceSubdomain';

export const assertValidSubdomain = (environmentInfo: IEnvironment) => {
    if (isValidCodespaceSubdomain(environmentInfo, config.environment)) {
        return;
    }

    sendTelemetry(
        'vsonline/workbench/bydesign-error/subdomain-mismatch',
        {
            environment: config.environment,
            topLevelDomain: getParentDomain(location.href),
        },
    );

    throw new SubdomainMismatchError('The Codespace does not belong here.');
};
