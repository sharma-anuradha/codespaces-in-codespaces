import {
    ICloudEnvironment,
    CreateEnvironmentParameters as CreateEnvironmentParametersBase,
    StateInfo,
    EnvPersonalization,
} from '../interfaces/cloudenvironment';

import { useWebClient } from '../actions/middleware/useWebClient';
import { useActionContext } from '../actions/middleware/useActionContext';
import { pollActivatingEnvironment } from '../actions/pollEnvironment';
import { isActivating, isNotAvailable } from '../utils/environmentUtils';
import { wait } from '../dependencies';
import { evaluateFeatureFlag, customContainers } from '../utils/featureSet';

// Webpack configuration enforces isolatedModules use on typescript
// and prevents direct re-exporting of types.
export type CreateEnvironmentParameters = CreateEnvironmentParametersBase;

export async function fetchEnvironments(): Promise<ICloudEnvironment[]> {
    const { configuration } = useActionContext().state;
    if (!configuration) {
        throw new Error('Configuration must be fetched before calling EnvReg service.');
    }

    const emptyEnvironmentList: ICloudEnvironment[] = [];

    const { environmentRegistrationEndpoint } = configuration;
    const webClient = useWebClient();

    const fetchedEnvironments = await webClient.get(environmentRegistrationEndpoint);
    if (!Array.isArray(fetchedEnvironments)) {
        return emptyEnvironmentList;
    }

    fetchedEnvironments.forEach((environment) => {
        environment.active = new Date(environment.active);
        environment.created = new Date(environment.created);
        environment.updated = new Date(environment.updated);
    });

    return fetchedEnvironments.sort((a: ICloudEnvironment, b: ICloudEnvironment) => {
        return b.updated.getTime() - a.updated.getTime();
    });
}

export async function createEnvironment(
    environment: CreateEnvironmentParameters
): Promise<ICloudEnvironment> {
    const configuration = useActionContext().state.configuration;
    const containers = evaluateFeatureFlag(customContainers);

    if (!configuration) {
        throw new Error('Configuration must be fetched before calling EnvReg service.');
    }

    const { environmentRegistrationEndpoint } = configuration;
    const {
        friendlyName,
        planId,
        location,
        gitRepositoryUrl,
        type = 'cloudEnvironment',
        userEmail,
        userName,
        dotfilesRepository,
        dotfilesInstallCommand,
        dotfilesTargetPath = `~/dotfiles`,
        autoShutdownDelayMinutes,
        skuName,
    } = environment;

    const personalization: EnvPersonalization = {
        dotfilesRepository,
        dotfilesTargetPath,
        dotfilesInstallCommand,
    };

    const body = {
        id: '',
        type,
        planId,
        location,
        friendlyName,
        experimentalFeatures: {
            customContainers: containers,
        },
        seed: {
            type: gitRepositoryUrl ? 'git' : '',
            moniker: gitRepositoryUrl ? gitRepositoryUrl : '',
            gitConfig: { userName, userEmail },
        },
        personalization,
        state: StateInfo.Provisioning,
        connection: {
            sessionId: '',
            sessionPath: '',
        },
        created: new Date(),
        autoShutdownDelayMinutes,
        skuName,
    };

    const webClient = useWebClient();
    return await webClient.post(environmentRegistrationEndpoint, body);
}

export async function getEnvironment(id: string): Promise<ICloudEnvironment | undefined> {
    const configuration = useActionContext().state.configuration;
    if (!configuration) {
        throw new Error('Configuration must be fetched before calling EnvReg service.');
    }

    const { environmentRegistrationEndpoint } = configuration;
    const webClient = useWebClient();

    return await webClient.get(`${environmentRegistrationEndpoint}/${id}?t=${Date.now()}`, {
        retryCount: 2,
    });
}

export async function deleteEnvironment(id: string): Promise<void> {
    const configuration = useActionContext().state.configuration;
    if (!configuration) {
        throw new Error('Configuration must be fetched before calling EnvReg service.');
    }

    const { environmentRegistrationEndpoint } = configuration;
    const webClient = useWebClient();

    await webClient.delete(`${environmentRegistrationEndpoint}/${id}`, {
        retryCount: 2,
    });
}

export async function shutdownEnvironment(id: string): Promise<void> {
    const actionContext = useActionContext();
    const configuration = actionContext.state.configuration;
    if (!configuration) {
        throw new Error('Configuration must be fetched before calling EnvReg service.');
    }

    const { environmentRegistrationEndpoint } = configuration;
    const webClient = useWebClient();

    await webClient.post(`${environmentRegistrationEndpoint}/${id}/shutdown`, null, {
        retryCount: 2,
    });

    await pollActivatingEnvironment(id);

    // And then wait for status to change
    let environmentNotSuspended = true;
    while (environmentNotSuspended) {
        const env = actionContext.state.environments.environments.find((env) => env.id === id);
        if (!env) {
            throw new Error('InvalidId');
        }

        environmentNotSuspended = isActivating(env);
        await wait(1000);
    }
}

export async function connectEnvironment(
    id: string,
    state: StateInfo
): Promise<ICloudEnvironment | undefined> {
    const actionContext = useActionContext();
    const configuration = actionContext.state.configuration;
    if (!configuration) {
        throw new Error('Configuration must be fetched before calling EnvReg service.');
    }

    if (state === StateInfo.Shutdown) {
        const { environmentRegistrationEndpoint } = configuration;
        const webClient = useWebClient();
        await webClient.post(`${environmentRegistrationEndpoint}/${id}/start`, null, {
            retryCount: 2,
        });

        // We start polling
        await pollActivatingEnvironment(id);

        // And then wait for status to change
        let environmentNotAvailable = true;
        while (environmentNotAvailable) {
            await wait(1000);

            const env = actionContext.state.environments.environments.find((env) => env.id === id);
            if (!env) {
                throw new Error('InvalidId');
            }

            environmentNotAvailable = isNotAvailable(env);
        }
    }

    return await getEnvironment(id);
}
