
class Loader {

    private loadPromise: Promise<boolean>;
    private amdRequire: any;

    constructor() {
        this.loadPromise = new Promise((resolve, reject) => {
            this.init(resolve);
        });
    }

    init(callback: () => void) {
        const blb = './static/vscode';
        const scheme = window.location.protocol;

        this.loadScript(`${blb}/vs/loader.js`, () => {
            this.amdRequire = eval('require');

            this.amdRequire.config({
                baseUrl: `${scheme}${blb}`
            });

            callback();
        });
    }

    private loadScript(path: string, callback: any) {
        const script = document.createElement('script');
        script.onload = callback;
        script.async = true;
        script.type = 'text/javascript';
        script.src = path;
        document.head.appendChild(script);
    }

    async loadModule(mod: string): Promise<any> {
        return this.loadModules([mod]);
    }

    async loadModules(mods: string[]): Promise<any> {
        await this.loadPromise;

        return new Promise((resolve, reject) => {
            this.amdRequire(mods, (args: any) => {
                resolve(args);
            });
        })
    }

    async loadWorkbench(): Promise<any> {
        await this.loadModule('vs/workbench/workbench.nodeless.main');
        const workbench = await this.loadModule('vs/workbench/browser/nodeless.main');
        return workbench.main();
    }
}

export const loader = new Loader();