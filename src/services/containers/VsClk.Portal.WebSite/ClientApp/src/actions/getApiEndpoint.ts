import { IEnvironment } from 'vso-client-core';

import { useActionContext } from './middleware/useActionContext';

export const getApiEndpoint = (environmentInfo: IEnvironment) => {
    const { state } = useActionContext();
    const { configuration, locations } = state;

    if (!configuration) {
        throw new Error('No configuration is set.');
    }
    
    const { environmentRegistrationEndpoint, environmentsApiPath } = configuration;
    const { location } = environmentInfo;

    // if no dns config, return the general registration endpoint
    if (!locations) {
        return environmentRegistrationEndpoint;
    }

    const { hostnames } = locations;
    if (!hostnames) {
        return environmentRegistrationEndpoint;
    }

    const dnsRecord = hostnames[location];

    // if no location record found, return the general registration endpoint
    if (typeof dnsRecord !== 'string' || !dnsRecord) {
        return environmentRegistrationEndpoint;
    }

    const stampUrl = new URL(environmentsApiPath, `https://${dnsRecord}`);

    return stampUrl.toString();
};
