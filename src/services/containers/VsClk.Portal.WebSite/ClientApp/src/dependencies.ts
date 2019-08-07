import { v4 } from 'uuid';

export function createUniqueId(): string {
    return v4();
}

export function wait(ms: number) {
    return new Promise((resolve) => {
        // tslint:disable-next-line: no-string-based-set-timeout
        setTimeout(resolve, ms);
    });
}
