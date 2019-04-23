import { AuthService } from './authService';
import { ICloudEnvironment } from '../interfaces/cloudenvironment';

export default class EnvRegService {

    //private static servicePath = '/api/environment'; // For production
    private static servicePath = '/api'; // Comment out for local development

    private static async getToken(): Promise<string> {
        return await AuthService.Instance.getToken();
    }

    private static async get(url: string): Promise<Response> {
        const token = await EnvRegService.getToken();
        if (token) {
            return fetch(url, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json' 
                },
                credentials: 'include'
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
            });
        }
        return undefined;
    }

    static fetchEnvironments(): Promise<ICloudEnvironment[]> {
        let env: ICloudEnvironment[] = [];
        const isAuthenticated = AuthService.Instance.isAuthenticated();
        if (!isAuthenticated) return Promise.resolve([]);
        return this.get(`${EnvRegService.servicePath}/registration`)
            .then(response => {
                if (response && response.status === 200) {
                    return response.text().then((data) => {
                        return JSON.parse(data);
                    })
                } else {
                    throw new Error(response ? response.statusText : 'Error fetching environments');
                }
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
            })
    }


    static newEnvironment(name: string): Promise<ICloudEnvironment> {
        return this.post(`${EnvRegService.servicePath}/registration`, {
            friendlyName: name
        })
            .then(response => {
                if (response && response.status === 200) {
                    return response.json();
                } else {
                    throw 'Error creating new environment';
                }
            });
    }

    static getEnvironment(id: string): Promise<ICloudEnvironment> {
        return this.get(`${EnvRegService.servicePath}/registration/${id}`)
            .then(response => response.json())
    }
}