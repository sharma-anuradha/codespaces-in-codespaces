const defer = (fn: Function, timeout: number) => {
    const timer = setTimeout(fn, timeout);

    return timer;
};
