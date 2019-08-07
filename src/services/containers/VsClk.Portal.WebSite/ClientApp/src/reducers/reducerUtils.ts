// tslint:disable-next-line: export-name
export function replaceAtIndex<T>(array: T[], index: number, value: T): T[] {
    return [...array.slice(0, index), value, ...array.slice(index + 1)];
}
