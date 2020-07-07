import { ajax } from 'Fx/Ajax';

export class HttpClient {
    private token: string;

    public withToken(token: string): HttpClient {
        this.token = token;
        return this;
    }

    public get<T>(
        uri: string,
        options: { headers?: any; success?: any; error?: any } = {}
    ): Q.Promise<T> {
        if (!options.headers) {
            options.headers = this.getBaseHeaders();
        }

        return ajax<T>({
            type: 'GET',
            crossDomain: true,
            headers: options.headers,
            uri,
            useRawAjax: true,
            success: options.success,
            error: options.error,
        });
    }

    public post<T>(uri: string, data: any = {}): Q.Promise<T> {
        return ajax<T>({
            type: 'POST',
            crossDomain: true,
            headers: this.getBaseHeaders(),
            uri,
            data,
        });
    }

    public patch<T>(uri: string, data: any = {}): Q.Promise<T> {
        return ajax<T>({
            type: 'PATCH',
            crossDomain: true,
            headers: this.getBaseHeaders(),
            uri,
            data,
        });
    }

    public delete(uri: string): Q.Promise<void> {
        return ajax({
            type: 'DELETE',
            crossDomain: true,
            headers: this.getBaseHeaders(),
            uri,
        });
    }

    private getBaseHeaders(): { [key: string]: string } {
        const headers: { [key: string]: string } = {
            'Content-Type': 'application/json',
        };
        if (this.token) {
            headers.Authorization = `Bearer ${this.token}`;
        }
        return headers;
    }
}
