import { IWorkbench } from 'vscode-web';

let vscodeInternal: IWorkbench;


interface IObjectWithVSCodeInit extends Object {
    getVSCode(): Promise<IWorkbench>;
}

interface IWorkbenchWithInit extends IObjectWithVSCodeInit, IWorkbench {}

const proxyHandler = {
    get(target: IObjectWithVSCodeInit, name: keyof IWorkbenchWithInit) {
        if (name === 'getVSCode') {
            return target[name];
        }

        if (!vscodeInternal) {
            throw new Error(`Please call "await getVSCode" before accessing the "${name}" variable, to fetch the vscode library.`);
        }

        if (name in vscodeInternal) {
            return vscodeInternal[name];
        }
    }
};

declare var AMDLoader: any;
export const vscode = new Proxy({
    getVSCode(): Promise<IWorkbench> {
        return new Promise((resolve) => {
            AMDLoader.global.require(['vs/workbench/workbench.web.api'], (workbench: IWorkbench) => {
                vscodeInternal = workbench;
                resolve(workbench);
            });
        });
    }
}, proxyHandler) as IWorkbenchWithInit;