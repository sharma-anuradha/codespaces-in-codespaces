export const injectMessageParameters = (message: string, ...args: any[]): string => {
    for (let i = 0; i < args.length; i++) {
        message = message.replace('{}', args[i]);
    }
    return message;
};

export const injectMessageParametersJSX = (message: string, ...args: any[]): string[] => {
    const strings = message.split('{}');
    for(let i = 1, argCount = 0; i < strings.length; i += 2, argCount++) {
        strings.splice(i, 0, args[argCount])
    }
    return strings;
};
