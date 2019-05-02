import { AuthService } from './authService';
import { ICloudEnvironment } from '../interfaces/cloudenvironment';

export default class EnvRegService {

    private static servicePath = '/api/environment';

    private static async getToken(): Promise<string> {
        return await AuthService.Instance.getToken();
    }

    private static async get(url: string): Promise<Response> {
        const token = await EnvRegService.getToken();
        if (token) {
            return fetch(url, {
                method: 'GET',
                credentials: 'include',
                headers: {
                    'Content-Type': 'application/json'
                }
            }).then((response) => {
                if (!response) {
                    throw new Error(`GET to ${url} with returned an empty response`);
                }
                if (response.status !== 200) {
                    throw new Error(response.statusText);
                }
                return response;
            });
        }
        return undefined;
    }

    private static async post(url: string, data: any): Promise<Response> {
        const token = await EnvRegService.getToken();
        if (token) {
            return fetch(url, {
                method: 'POST',
                credentials: 'include',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(data)
            }).then((response) => {
                if (!response) {
                    throw new Error(`POST to ${url} with data ${JSON.stringify(data)} returned an empty response`);
                }
                if (response.status !== 200) {
                    throw new Error(response.statusText);
                }
                return response;
            });
        }
        return undefined;
    }

    static async fetchEnvironments(): Promise<ICloudEnvironment[]> {
        let env: ICloudEnvironment[] = [];
        const isAuthenticated = await AuthService.Instance.isAuthenticated();
        if (!isAuthenticated) return [];
        return this.get(`${EnvRegService.servicePath}/registration`)
            .then(response => {
                return response.text().then((data) => {
                    return JSON.parse(data);
                })
            })
            .then((environments: ICloudEnvironment[]) => {
                if (environments) {
                    if (Array.isArray(environments)) {
                        // Convert dates. 
                        environments.forEach(environment => {
                            environment.active = new Date(environment.active);
                            environment.created = new Date(environment.created);
                            environment.updated = new Date(environment.updated);
                        });
                        env = environments;
                        // Order them by updated time DESC
                        environments.sort((a: ICloudEnvironment, b: ICloudEnvironment) => {
                            return b.updated.getTime() - a.updated.getTime();
                        })
                    }
                }
                return env;
            }).catch(() => {
                throw 'Error fetching environments';
            })
    }


    static newEnvironment(name: string): Promise<ICloudEnvironment> {
        return this.post(`${EnvRegService.servicePath}/registration`, {
            friendlyName: name
        })
            .then(response => {
                return response.json();
            }).catch((e) => {
                throw 'Error creating new environment';
            });
    }

    static getEnvironment(id: string): Promise<ICloudEnvironment> {
        return this.get(`${EnvRegService.servicePath}/registration/${id}`)
            .then(response => response.json())
    }
}