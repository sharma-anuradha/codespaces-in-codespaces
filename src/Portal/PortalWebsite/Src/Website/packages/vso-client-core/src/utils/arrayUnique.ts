export const arrayUnique = <T>(arr: T[]): T[] => {
    const set = new Set(arr);

    return [...set];
};
