export function isThenable<T>(val: undefined | null | T | Promise<T>): val is Promise<T> {
    if (val == null) {
        return false;
    }
    if (typeof (val as Promise<T>).then !== 'function') {
        return false;
    }
    return true;
}
