export const isNumber = (thing: unknown): thing is number => {
    if (typeof thing !== 'number') {
        return false;
    }
    return !isNaN(thing);
};
