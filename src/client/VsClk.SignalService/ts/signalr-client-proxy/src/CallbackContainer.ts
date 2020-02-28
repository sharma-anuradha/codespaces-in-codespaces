import { IDisposable } from './IDisposable';

export class CallbackContainer<T> {
    private callbacks: Array<T> = [];

    public get items() {
        return this.callbacks;
    }

    public add(callback: T): IDisposable {
        if (callback) {
            this.callbacks.push(callback);
        }

        return {
            dispose: () => {
                const index = this.callbacks.indexOf(callback);
                if (index !== -1) {
                    this.callbacks.splice(index, 1);
                }
            }
        };
    }
}