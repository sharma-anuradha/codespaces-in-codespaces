
import { trace as baseTrace } from '../utils/trace';

export const createTrace = (name: string) => {
    const verbose = baseTrace.extend(`${name}:trace`);
    verbose.log =
        // tslint:disable-next-line: no-console
        typeof console.debug === 'function' ? console.debug.bind(console) : console.log.bind(console);

    const info = baseTrace.extend(`${name}:info`);
    info.log =
        // tslint:disable-next-line: no-console
        console.info.bind(console);

    const warn = baseTrace.extend(`${name}:warn`);
    warn.log =
        // tslint:disable-next-line: no-console
        console.warn.bind(console);

    const error = baseTrace.extend(`${name}:error`);
    error.log =
        // tslint:disable-next-line: no-console
        console.warn.bind(console);

    return {
        verbose,
        info,
        warn,
        error
    }
}