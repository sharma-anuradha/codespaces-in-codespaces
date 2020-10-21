/**
 * Function to render time in milliseconds as seconds string.
 * e.g. 2020ms => `2.02s`
 */
export const seconds = (timeMs: number, decimal = 2): string => {
    const timeSec = timeMs / 1000;
    return `${timeSec.toFixed(decimal)}s`;
}
