export function clamp<T>(array: T[], length: number): T[] {
    if (array.length <= length) {
        return array.slice(0);
    }

    return array.slice(0, length);
}
