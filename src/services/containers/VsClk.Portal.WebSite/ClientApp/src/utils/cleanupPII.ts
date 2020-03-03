import { useActionContext } from '../actions/middleware/useActionContext';

/**
 * Function to replace all paths with [PATH] label.
 */
export const obfusticatePaths = (filePath: string, replacementString = ''): string => {
   return filePath.replace(/([A-Za-z]:)?(\S*[\\\/])+\S*/gi, (match: string, drive: string, directory: string, offset: number, whole: string) => {
       if (/^\d{1,4}\/\d{1,2}\/\d{1,4}$/.test(match)) { // This is a date. No need to scrub.
           return match;
       } else {
           const driveAndDirectoryLength = (drive ? drive.length : 0) + directory.length;
           const fileName = match.substr(driveAndDirectoryLength);
           return replacementString + fileName;
       }
   });
}

/**
 *  Function to replace all emails with `[EMAIL]` label. 
 */
export const obfusticateEmailAddresses = (str: string): string => {
    return str.replace(/[\S]+@[\S]+/gi, '[EMAIL]');
}

/**
 *  Function to remove PII from a string.
 */
export const cleanupPII = (str: string | undefined): string | undefined => {
    if (!str) {
        return str;
    }

    return obfusticateEmailAddresses(obfusticatePaths(str, '[PATH]/'));
}

export const cleanupPIIForExternal = (str?: string): string | undefined => {
    const context = useActionContext();
    const { isInternal } = context.state.authentication;

    if (isInternal) {
        return str;
    }

    return cleanupPII(str);
}
