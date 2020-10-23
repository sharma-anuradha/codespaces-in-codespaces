import debug from 'debug';

const defaultLogger = debug.default('vscs');

// if (process.env.NODE_ENV === 'development') {
debug.enable('vscs:*');
// }

export function createLogger(namespace?: string) {
    let logger;
    if (!namespace) {
        logger = {
            verbose: defaultLogger.extend('verbose'),
            info: defaultLogger.extend('info'),
            warn: defaultLogger.extend('warn'),
            error: defaultLogger.extend('error'),
        };
    } else {
        logger = {
            verbose: defaultLogger.extend(`${namespace}:verbose`),
            info: defaultLogger.extend(`${namespace}:info`),
            warn: defaultLogger.extend(`${namespace}:warn`),
            error: defaultLogger.extend(`${namespace}:error`),
        };
    }

    // tslint:disable-next-line: no-console
    logger.verbose.log = console.debug ? console.debug.bind(console) : console.info.bind(console);
    // tslint:disable-next-line: no-console
    logger.info.log = console.info.bind(console);
    // tslint:disable-next-line: no-console
    logger.warn.log = console.warn.bind(console);
    // tslint:disable-next-line: no-console
    logger.error.log = console.error.bind(console);

    return logger;
}

export type Logger = ReturnType<typeof createLogger>;
