import {
    ICloudEnvironment,
    CreateEnvironmentParameters as CreateEnvironmentParametersBase,
    StateInfo,
    EnvPersonalization,
} from '../interfaces/cloudenvironment';

import { useWebClient } from '../actions/middleware/useWebClient';
import { useActionContext } from '../actions/middleware/useActionContext';
import { pollEnvironment } from '../actions/pollEnvironment';

// Webpack configuration enforces isolatedModules use on typescript
// and prevents direct re-exporting of types.
export type CreateEnvironmentParameters = CreateEnvironmentParametersBase; 

export async function fetchEnvironments(): Promise<ICloudEnvironment[]> {
    const configuration = useActionContext().state.configuration;
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
        seed: {
            type: gitRepositoryUrl ? 'git' : '',
            moniker: gitRepositoryUrl ? gitRepositoryUrl : '',
            gitConfig: { userName, userEmail },
        },
        personalization,
        state: StateInfo.Creating,
        connection: {
            sessionId: '',
            sessionPath: '',
        },        
        created: new Date(),
        autoShutdownDelayMinutes,
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

    return await webClient.get(`${environmentRegistrationEndpoint}/${id}`);
}

export async function deleteEnvironment(id: string): Promise<void> {
    const configuration = useActionContext().state.configuration;
    if (!configuration) {
        throw new Error('Configuration must be fetched before calling EnvReg service.');
    }

    const { environmentRegistrationEndpoint } = configuration;
    const webClient = useWebClient();

    await webClient.delete(`${environmentRegistrationEndpoint}/${id}`);
}

export async function shutdownEnvironment(id: string): Promise<void> {
    const configuration = useActionContext().state.configuration;
    if (!configuration) {
        throw new Error('Configuration must be fetched before calling EnvReg service.');
    }

    const { environmentRegistrationEndpoint } = configuration;
    const webClient = useWebClient();

    await webClient.post(`${environmentRegistrationEndpoint}/${id}/shutdown`, null);
}

export async function connectEnvironment(id: string, state: StateInfo): Promise<ICloudEnvironment | undefined> {
    const configuration = useActionContext().state.configuration;
    if (!configuration) {
        throw new Error('Configuration must be fetched before calling EnvReg service.');
    }

    const { environmentRegistrationEndpoint } = configuration;
    const webClient = useWebClient();

    if (state === StateInfo.Shutdown) {
        await webClient.post(`${environmentRegistrationEndpoint}/${id}/start`, null);
        await pollEnvironment(id, StateInfo.Available);
    }

    return await getEnvironment(id);
}
