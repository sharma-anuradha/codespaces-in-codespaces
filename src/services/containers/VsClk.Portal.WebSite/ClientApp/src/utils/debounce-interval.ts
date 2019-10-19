type Func = (...args: any[]) => any;
type ReplaceReturnType<T extends Func, TNewReturn> = (...a: Parameters<T>) => TNewReturn;

/**
 * Interval version of a debounce util.
 */
export const debounceInterval = <A extends Func>(fn: A, timeout: number): ReplaceReturnType<A, void> & { stop: () => void; }  => {
    let interval: ReturnType<typeof setInterval> | undefined;

    const debounced = function (...args: any[]) {
        clearInterval(interval!);

        interval = setInterval(() => {
            fn(...args);
        }, timeout);
    }

    debounced.stop = () => {
        clearInterval(interval!);
    }

    return debounced;
}
