import { CodespacesManager } from './CodespacesManager';
import { Codespace, Location, isTransient } from './CodespaceModels';
import { HttpClient } from '../../../Shared/HttpClient';
import { getCodespacesUri, getArmUri } from '../../../Shared/Endpoints';
import { ajax } from 'Fx/Ajax';

export class HttpCodespacesManager implements CodespacesManager {
    private tokenPromise: Q.Promise<string>;
    private readonly planId: string;
    private readonly pollingPromises: { [key: string]: Q.Promise<boolean> } = {};

    constructor(planId: string) {
        this.planId = planId;
        this.tokenPromise = ajax<{ accessToken: string }>({
            setAuthorizationHeader: true,
            type: 'POST',
            uri: getArmUri(`${planId}/writeCodespaces`),
        }).then(({ accessToken }) => accessToken);
    }

    fetchLocation(id: string): Q.Promise<Location> {
        return this.getHttpClient().then((client) =>
            client.get<Location>(getCodespacesUri(`locations/${id}`))
        );
    }

    fetchCodespaces(): Q.Promise<Codespace[]> {
        return this.getHttpClient().then((client) =>
            client.get<Codespace[]>(getCodespacesUri('environments'))
        );
    }

    fetchCodespace(id: string): Q.Promise<Codespace> {
        return this.getHttpClient().then((client) =>
            client.get<Codespace>(getCodespacesUri(`environments/${id}`))
        );
    }

    createCodespace(
        codespace: Omit<Codespace, 'type' | 'id' | 'state' | 'planId'>
    ): Q.Promise<Codespace> {
        const body = {
            ...codespace,
            type: 'CloudEnvironment',
            planId: this.planId,
        };

        return this.getHttpClient().then((client) =>
            client.post<Codespace>(getCodespacesUri('environments'), JSON.stringify(body))
        );
    }

    pollTransitioningCodespace(id: string, useCache: boolean = true): Q.Promise<boolean> {
        const cachedPromise = this.pollingPromises[id];

        if (useCache && cachedPromise && cachedPromise.isPending()) {
            return cachedPromise;
        }

        const pollingPromise = this.fetchCodespace(id).then(
            (codespace) =>
                !isTransient(codespace) ||
                Q.delay(1000).then(() => this.pollTransitioningCodespace(id, false))
        );

        this.pollingPromises[id] = pollingPromise;
        return pollingPromise;
    }

    suspendCodespace(id: string): Q.Promise<void> {
        return this.getHttpClient().then((client) =>
            client.post(getCodespacesUri(`environments/${id}/shutdown`))
        );
    }

    deleteCodespace(id: string): Q.Promise<void> {
        return this.getHttpClient().then((client) =>
            client.delete(getCodespacesUri(`environments/${id}`))
        );
    }

    editCodespace(id: string, skuName: string, autoShutdownDelayMinutes: number): Q.Promise<void> {
        return this.getHttpClient().then((client) =>
            client.patch(
                getCodespacesUri(`environments/${id}`),
                JSON.stringify({
                    id,
                    skuName,
                    autoShutdownDelayMinutes,
                })
            )
        );
    }

    private getHttpClient(): Q.Promise<HttpClient> {
        return this.tokenPromise.then((token) => new HttpClient().withToken(token));
    }
}
