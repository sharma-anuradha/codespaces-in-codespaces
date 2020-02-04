type Func = (...args: any[]) => any;
type ReplaceReturnType<T extends Func, TNewReturn> = (...a: Parameters<T>) => TNewReturn;

interface IRafTimer {
    request:  undefined | number;
    isEnded: boolean;
}

/**
 * The `setInterval` implementation using RequestAnimationFrame API.
 * The reason ffor using rAF is that it gets suspended when the tab is not active.
 * Hence we keep the debounce alive only if user is active.
 * @param fn Callback.
 * @param interval Interval in milliseconds.
 */
export const setRafInterval = (fn: Function, interval: number): IRafTimer => {
    let timer: IRafTimer = {
        request: undefined,
        isEnded: false
    };

    let before: number;
    function check() {
        if (timer.isEnded) {
            return;
        }

        timer.request = requestAnimationFrame(check);

        let now = Date.now();

        if (now - before >= interval) {
            before = now;
            fn();
        }
    }

    before = Date.now();
    timer.request = requestAnimationFrame(check);
    
    return timer;
}

/**
 * The `clearInterval` implementation using RequestAnimationFrame API
 * for the `setRafInterval` function above.
 * @param timer 
 */
export const clearRafInterval = (timer: IRafTimer) => {
    if (!timer) {
        return;
    }

    timer.isEnded = true;

    if (timer.request) {
        cancelAnimationFrame(timer.request);
    }
}

/**
 * Interval version of a debounce util.
 */
export const debounceInterval = <A extends Func>(fn: A, timeout: number): ReplaceReturnType<A, void> & { stop: () => void; }  => {
    let interval: ReturnType<typeof setRafInterval> | undefined;

    const debounced = function (...args: any[]) {
        clearRafInterval(interval!);

        interval = setRafInterval(() => {
            fn(...args);
        }, timeout);
    }

    debounced.stop = () => {
        clearRafInterval(interval!);
    }

    return debounced;
}
