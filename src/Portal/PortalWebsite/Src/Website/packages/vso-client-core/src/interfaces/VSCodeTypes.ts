import { IWorkbench, URI } from 'vscode-web';

let vscodeInternal: IWorkbench;

interface IObjectWithVSCodeInit extends Object {
    getVSCode(): Promise<IWorkbench>;
}

interface IWorkbenchWithInit extends IObjectWithVSCodeInit, IWorkbench { }

const proxyHandler = {
    get(target: IObjectWithVSCodeInit, name: keyof IWorkbenchWithInit) {
        if (name === 'getVSCode') {
            return target[name];
        }

        if (!vscodeInternal) {
            throw new Error(
                `Please call "await getVSCode" before accessing the "${name}" variable, to fetch the vscode library.`
            );
        }

        if (name in vscodeInternal) {
            return vscodeInternal[name];
        }
    },
};

declare var AMDLoader: any;
export const vscode = new Proxy(
    {
        getVSCode(): Promise<IWorkbench> {
            return new Promise((resolve) => {
                AMDLoader.global.require(
                    ['vs/workbench/workbench.web.api'],
                    (workbench: IWorkbench) => {
                        vscodeInternal = workbench;
                        resolve(workbench);
                    }
                );
            });
        },
    },
    proxyHandler
) as IWorkbenchWithInit;

export enum FileType {
    Unknown = 0,
    File = 1,
    Directory = 2,
    SymbolicLink = 64,
}

export enum FileSystemProviderCapabilities {
    FileReadWrite = 1 << 1,
    FileOpenReadWriteClose = 1 << 2,
    FileFolderCopy = 1 << 3,

    PathCaseSensitive = 1 << 10,
    Readonly = 1 << 11,

    Trash = 1 << 12,
}

export class FileSystemError extends Error {
    static FileExists(messageOrUri?: string | URI): FileSystemError {
        return new FileSystemError(
            messageOrUri,
            FileSystemProviderErrorCode.FileExists,
            FileSystemError.FileExists
        );
    }
    static FileNotFound(messageOrUri?: string | URI): FileSystemError {
        return new FileSystemError(
            messageOrUri,
            FileSystemProviderErrorCode.FileNotFound,
            FileSystemError.FileNotFound
        );
    }
    static FileNotADirectory(messageOrUri?: string | URI): FileSystemError {
        return new FileSystemError(
            messageOrUri,
            FileSystemProviderErrorCode.FileNotADirectory,
            FileSystemError.FileNotADirectory
        );
    }
    static FileIsADirectory(messageOrUri?: string | URI): FileSystemError {
        return new FileSystemError(
            messageOrUri,
            FileSystemProviderErrorCode.FileIsADirectory,
            FileSystemError.FileIsADirectory
        );
    }
    static NoPermissions(messageOrUri?: string | URI): FileSystemError {
        return new FileSystemError(
            messageOrUri,
            FileSystemProviderErrorCode.NoPermissions,
            FileSystemError.NoPermissions
        );
    }
    static Unavailable(messageOrUri?: string | URI): FileSystemError {
        return new FileSystemError(
            messageOrUri,
            FileSystemProviderErrorCode.Unavailable,
            FileSystemError.Unavailable
        );
    }

    constructor(
        uriOrMessage?: string | URI,
        code: FileSystemProviderErrorCode = FileSystemProviderErrorCode.Unknown,
        terminator?: Function
    ) {
        super(vscode.URI.isUri(uriOrMessage) ? uriOrMessage.toString() : uriOrMessage);

        // mark the error as file system provider error so that
        // we can extract the error code on the receiving side
        markAsFileSystemProviderError(this, code);

        // workaround when extending builtin objects and when compiling to ES5, see:
        // https://github.com/Microsoft/TypeScript-wiki/blob/master/Breaking-Changes.md#extending-built-ins-like-error-array-and-map-may-no-longer-work
        if (typeof (<any>Object).setPrototypeOf === 'function') {
            (<any>Object).setPrototypeOf(this, FileSystemError.prototype);
        }

        if (typeof (Error as any).captureStackTrace === 'function' && typeof terminator === 'function') {
            // nice stack traces
            Error.captureStackTrace(this, terminator);
        }
    }
}

export function markAsFileSystemProviderError(
    error: Error,
    code: FileSystemProviderErrorCode
): Error {
    error.name = code ? `${code} (FileSystemError)` : `FileSystemError`;

    return error;
}

export enum FileSystemProviderErrorCode {
    FileExists = 'EntryExists',
    FileNotFound = 'EntryNotFound',
    FileNotADirectory = 'EntryNotADirectory',
    FileIsADirectory = 'EntryIsADirectory',
    NoPermissions = 'NoPermissions',
    Unavailable = 'Unavailable',
    Unknown = 'Unknown',
}

export class FileSystemProviderError extends Error {
    constructor(message: string, public readonly code: FileSystemProviderErrorCode) {
        super(message);
    }
}

/**
 * Possible changes that can occur to a file.
 */
export enum FileChangeType {
    UPDATED = 0,
    ADDED = 1,
    DELETED = 2,
}


