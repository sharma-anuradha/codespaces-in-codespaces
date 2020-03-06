import { randomString } from './randomString';

export class CrossDomainPFAuthenticator {
    private id: string;
    private formEl: HTMLFormElement;
    private iframeEl: HTMLIFrameElement;
    private requestTimer: ReturnType<typeof setTimeout> | undefined;

    constructor(private endpoint: string) {
        this.id = randomString();
        this.iframeEl = this.createIframe();
        this.formEl = this.createForm();
    }

    private getIframeName = () => {
        return `vso-pf-auth-iframe-${this.id}`;
    };

    private hideElement = (el: HTMLElement) => {
        el.setAttribute('style', 'position: absolute; z-index: -1; left: -100%; opacity: 0;');
    };

    private createIframe = (): HTMLIFrameElement => {
        const iframeEl = document.createElement('iframe');
        iframeEl.setAttribute('name', this.getIframeName());
        this.hideElement(iframeEl);

        document.body.appendChild(iframeEl);

        return iframeEl;
    };

    private createForm = (): HTMLFormElement => {
        const formEl = document.createElement('form');
        formEl.setAttribute('method', 'POST');
        formEl.setAttribute('target', this.getIframeName());

        const tokenInputEl = document.createElement('input');
        formEl.append(tokenInputEl);
        this.hideElement(formEl);
        document.body.appendChild(formEl);

        return formEl;
    };

    private resetDomElements = () => {
        if (this.requestTimer) {
            clearTimeout(this.requestTimer);
        }

        const tokenInputEl = this.formEl.querySelector('input');
        if (!tokenInputEl) {
            throw new Error('No input element found.');
        }

        tokenInputEl.setAttribute('value', '');
        this.iframeEl.setAttribute('src', 'about:blank');
    };

    private makeIframePostRequest = (pathName: string): Promise<void> => {
        return new Promise((res, rej) => {
            if (!this.formEl || !this.iframeEl) {
                throw new Error('Form or/and iframe element not found.');
            }

            this.formEl.setAttribute('action', `${this.endpoint}/${pathName}`);

            // set rejection timeout so the function call will not hang forever
            this.requestTimer = setTimeout(() => {
                this.resetDomElements();
                rej();
            }, 5000);

            this.iframeEl.onload = () => {
                this.resetDomElements();
                res();
            };
            
            this.formEl.submit();
        });
    };

    public setPFCookie = async (token: string, tokenName: string): Promise<void> => {
        if (!this.formEl || !this.iframeEl) {
            throw new Error('Form or/and iframe element not found.');
        }

        const tokenInputEl = this.formEl.querySelector('input');
        if (!tokenInputEl) {
            throw new Error('No input element found.');
        }
        // note that for AAD token case, the `name` should be `token`
        tokenInputEl.setAttribute('name', tokenName);
        tokenInputEl.setAttribute('value', token);

        return await this.makeIframePostRequest('authenticate-port-forwarder');
    };

    public removePFCookieWithCascadeToken = async (): Promise<void> => {
        return await this.makeIframePostRequest('logout-port-forwarder');
    };

    public dispose() {
        document.body.removeChild(this.formEl);
        document.body.removeChild(this.iframeEl);
    };
}
