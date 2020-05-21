import * as path from 'path';

import { Event, Emitter, Disposable } from 'vscode-jsonrpc';

import {
    IFileSystemProvider,
    IFileChange,
    URI,
    IWatchOptions,
    IStat,
    FileDeleteOptions,
    FileOverwriteOptions,
    FileWriteOptions,
} from 'vscode-web';

import { IndexedDBFS, IAsyncStorage, InMemoryAsyncStorage } from './indexedDBFS';
import {
    FileSystemError,
    FileChangeType,
    FileSystemProviderCapabilities,
    FileType,
    FileSystemProviderErrorCode,
} from '../../vscodeAssets/vscode';

const FILE_IS_DIRECTORY_MSG = 'EntryIsADirectory';

export class UserDataProvider implements IFileSystemProvider {
    readonly capabilities =
        FileSystemProviderCapabilities.FileReadWrite +
        FileSystemProviderCapabilities.PathCaseSensitive;

    private readonly onDidChangeCapabilitiesEmitter: Emitter<void> = new Emitter();
    public readonly onDidChangeCapabilities: Event<void> = this.onDidChangeCapabilitiesEmitter
        .event;
    private readonly onDidChangeFileEmitter: Emitter<IFileChange[]> = new Emitter();
    public readonly onDidChangeFile: Event<IFileChange[]> = this.onDidChangeFileEmitter.event;
    private readonly globalPath = path.join('/', 'User', 'state', 'global.json');
    private readonly userSettingsPath = path.join('/', 'User', 'settings.json');
    private storageProvider!: IAsyncStorage;

    public isFirstRun: boolean = false;

    constructor(private getDefaultSettings: () => Promise<string>) {}

    public async initializeDBProvider() {
        try {
            const storage = new IndexedDBFS();
            await storage.initialize();
            this.storageProvider = storage;
        } catch {
            this.storageProvider = new InMemoryAsyncStorage();
        }

        await this.initializeOptions();
    }

    private async initializeOptions() {
        const value = await this.storageProvider.getValue(this.globalPath);
        if (!value) {
            const options = [['workbench.telemetryOptOutShown', 'true']];
            await this.storageProvider.setValue(this.globalPath, JSON.stringify(options));

            const userSettingsValue = await this.storageProvider.getValue(this.userSettingsPath);
            if (!userSettingsValue) {
                await this.storageProvider.setValue(this.userSettingsPath, await this.getDefaultSettings());

                this.isFirstRun = true;
            }
        }
    }

    watch(resource: URI, opts: IWatchOptions): Disposable {
        return {
            dispose() {},
        };
    }

    async stat(resource: URI): Promise<IStat> {
        try {
            const content = await this.readFile(resource);
            return {
                type: FileType.File,
                ctime: 0,
                mtime: 0,
                size: content.byteLength,
            };
        } catch (e) {
            if (e.name && e.name.toString().startsWith(FILE_IS_DIRECTORY_MSG)) {
                return {
                    type: FileType.Directory,
                    ctime: 0,
                    mtime: 0,
                    size: 0,
                };
            }
        }
        throw new FileSystemError(resource, FileSystemProviderErrorCode.FileNotFound);
    }

    async mkdir(resource: URI): Promise<void> {
        await this.storageProvider.setValue(resource.path, 'directory');
    }

    async readdir(resource: URI): Promise<[string, FileType][]> {
        const directoryPath = resource.path + '/';
        const keys = await this.storageProvider.getAllKeys();
        const files: Map<string, [string, FileType]> = new Map<string, [string, FileType]>();

        for (const key of keys) {
            if (key.startsWith(directoryPath)) {
                const path = key.substring(directoryPath.length, key.length);
                if (path) {
                    const segments = path.split('/');
                    const file: [string, FileType] = [
                        segments[0], //Root name
                        segments.length === 1 ? FileType.File : FileType.Directory,
                    ];
                    files.set(segments[0], file);
                }
            }
        }
        return Array.from(files.values());
    }

    async delete(resource: URI, opts: FileDeleteOptions): Promise<void> {
        await this.storageProvider.deleteKey(resource.path);
    }

    async rename(from: URI, to: URI, opts: FileOverwriteOptions): Promise<void> {
        const value = await this.storageProvider.getValue(from.path);
        if (!value) {
            throw new FileSystemError(from, FileSystemProviderErrorCode.FileNotFound);
        }
        await this.storageProvider.deleteKey(from.path);
        await this.storageProvider.setValue(to.path, value);
    }

    async readFile(resource: URI): Promise<Uint8Array> {
        const value = await this.storageProvider.getValue(resource.path);
        if (!value) {
            throw new FileSystemError(resource, FileSystemProviderErrorCode.FileNotFound);
        }
        if (value === 'directory') {
            throw new FileSystemError(resource, FileSystemProviderErrorCode.FileIsADirectory);
        }

        return typeof TextEncoder !== undefined
            ? new TextEncoder().encode(value)
            : this.encode(value);
    }

    async writeFile(resource: URI, content: Uint8Array, opts: FileWriteOptions): Promise<void> {
        const value = await this.storageProvider.getValue(resource.path);
        if (value === 'directory') {
            throw new FileSystemError(resource, FileSystemProviderErrorCode.FileIsADirectory);
        }

        const decodedContent =
            typeof TextEncoder !== undefined
                ? new TextDecoder().decode(content)
                : this.decode(content);

        await this.storageProvider.setValue(resource.path, decodedContent);

        // We don't have the actual enum since it's defined as const enum - the any needs a fix in vscode codebase.
        this.onDidChangeFileEmitter.fire([{ resource, type: FileChangeType.UPDATED as any }]);
    }

    private encode(value: string): Uint8Array {
        var array = new Uint8Array(value.length);
        for (var i = 0; i < value.length; i++) array[i] = value.charCodeAt(i);
        return array;
    }

    private decode(buffer: Uint8Array): string {
        var array = Array.from(new Uint8Array(buffer));
        var value = String.fromCharCode.apply(String, array);
        return value;
    }
}
