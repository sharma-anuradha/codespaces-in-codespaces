import * as debug from 'debug';

const PACKAGE_NAME = 'vso';

export const trace = debug.default(PACKAGE_NAME);

// tslint:disable:no-console
export const createTrace = (name: string) => {
    const verbose = trace.extend(`${name}:trace`);
    verbose.log =
        // tslint:disable-next-line: no-console
        typeof console.debug === 'function'
            ? console.debug.bind(console)
            : console.log.bind(console);

    const info = trace.extend(`${name}:info`);
    info.log =
        // tslint:disable-next-line: no-console
        console.info.bind(console);

    const warn = trace.extend(`${name}:warn`);
    warn.log =
        // tslint:disable-next-line: no-console
        console.warn.bind(console);

    const error = trace.extend(`${name}:error`);
    error.log =
        // tslint:disable-next-line: no-console
        console.warn.bind(console);

    return {
        verbose,
        info,
        warn,
        error,
    };
};

export type Trace = ReturnType<typeof createTrace>;

// tslint:enable:no-console
export function maybePii<T>(val: T | undefined): T | string | undefined {
    if (val === undefined) {
        return undefined;
    }

    if (process.env.NODE_ENV === 'development') {
        return val;
    }

    return `Redacted <probably pii>`;
}
