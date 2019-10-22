
export const clampString = (str: string, limit: number) => {
    if (str.length <= limit) {
        return str;
    }

    return str.substr(0, limit-3) + '...';
}
