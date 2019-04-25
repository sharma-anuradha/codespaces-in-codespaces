interface AuthServiceResponse extends AuthUser {
}

interface AuthUser {
    name: string;
    email: string;
    accessToken: string;
}

export class AuthService {

    private static instance: AuthService;
    private user: AuthUser;

    private ready: Promise<void>;

    constructor() {
    }

    static get Instance() {
        if (!AuthService.instance) {
            AuthService.instance = new AuthService();
        }
        return AuthService.instance;
    }

    init() {
        this.ready = new Promise((resolve, reject) => {
            this.authorize().then(() => {
                resolve();
            });
        });
    }

    login() {

    }

    async logout() {
        await fetch('/signout', {
            method: 'POST'
        });
        this.user = undefined;
    }

    async getUser() {
        await this.ready;
        return this.user;
    }

    async isAuthenticated() {
        await this.ready;
        return !!this.user;
    }

    async getToken() {
        await this.ready;
        return this.user ? this.user.accessToken : undefined;
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
                        name: data.name,
                        email: data.email,
                        accessToken: data.accessToken   
                    };
                }
                return data;
            });
    }
}