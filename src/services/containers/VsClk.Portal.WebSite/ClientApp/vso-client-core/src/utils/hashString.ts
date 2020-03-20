

export const hashString = (str: string): string => {
    return str
        .split('')
        .reduce((a,b) => { 
            a=(((a << 5) - a) + b).charCodeAt(0);
            return a & a;
        },0)
        .toString();
}