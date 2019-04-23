interface AuthServiceResponse {
    name: string;
    accessToken: string;
}

interface AuthUser {
    name: string;
}

export class AuthService {

    private static instance: AuthService;
    private user: AuthUser;

    constructor() {
    }

    static get Instance() {
        if (!AuthService.instance) {
            AuthService.instance = new AuthService();
        }
        return AuthService.instance;
    }

    init() {
        this.authorize();
    }

    login() {

    }

    logout() {
        return fetch('/signout', {
            method: 'POST'
        }).then(() => {
            this.user = undefined;
        })
    }

    getUser() {
        return this.user;
    }

    isAuthenticated() {
        return !!this.user;
    }

    async getToken() {
        const authResponse = await this.authorize();
        return authResponse ? authResponse.accessToken : undefined;
    }

    private authorize(): Promise<AuthServiceResponse> {
        return fetch('/api/authorize')
            .then(response => response.text())
            .then(response => {
                try {
                    return JSON.parse(response);
                } catch (e) {
                    return undefined;
                }
            })
            .then(data => {
                if (data) {
                    this.user = {
                        name: data.name   
                    };
                }
                return data;
            });
    }
}