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
import {
	FileChangeType,
	FileType,
    FileSystemProviderCapabilities,
    FileSystemError,
	FileSystemProviderErrorCode,
} from './vscode';

import { IndexedDBFS } from './indexedDBFS'

const FILE_IS_DIRECTORY_MSG = 'File is a directory';
const FILE_NOT_FOUND_MSG = 'File not found';

export class UserDataProvider implements IFileSystemProvider {
	readonly capabilities = FileSystemProviderCapabilities.FileReadWrite + FileSystemProviderCapabilities.PathCaseSensitive;

    private onDidChangeCapabilitiesEmitter: Emitter<void> = new Emitter();
	public readonly onDidChangeCapabilities: Event<void> = this.onDidChangeCapabilitiesEmitter.event;
    private onDidChangeFileEmitter: Emitter<IFileChange[]> = new Emitter();
	public readonly onDidChangeFile: Event<IFileChange[]> = this.onDidChangeFileEmitter.event;

	private indexedDBFSProvider: IndexedDBFS;

	constructor () {
		this.indexedDBFSProvider = new IndexedDBFS();
	}

	public async initializeDBProvider() { 
		await this.indexedDBFSProvider.database;
	}

    watch(resource: URI, opts: IWatchOptions): Disposable {
        return {
            dispose() {}
		};
    }

    async stat(resource: URI): Promise<IStat> {
		try {
			const content = await this.readFile(resource);
			return {
				type: FileType.File,
				ctime: 0,
				mtime: 0,
				size: content.byteLength
			};
		} catch (e){
			if (e.message === FILE_IS_DIRECTORY_MSG) {
				return {
					type: FileType.Directory,
					ctime: 0,
					mtime: 0,
					size: 0
				};
			}
		}
		throw new FileSystemError(resource, FileSystemProviderErrorCode.FileNotFound);
	}
	
    async mkdir(resource: URI): Promise<void> {
		await this.indexedDBFSProvider.setValue(resource.path, 'directory');
	}
	
    async readdir(resource: URI): Promise<[string, FileType][]> {
		const directoryPath = resource.path + '/';
		const keys = await this.indexedDBFSProvider.getAllKeys();
		const files: Map<string, [string, FileType]> = new Map<string, [string, FileType]>();

		for (const key of keys) {
			if (key.startsWith(directoryPath)) {
				const path = key.substring(directoryPath.length, key.length);
				if (path) {
					const segments = path.split('/');
					const file: [string, FileType] = [
						segments[0], //Root name
						segments.length === 1 ? FileType.File : FileType.Directory
					]
					files.set(segments[0], file);
				}
			}
		}
		return Array.from(files.values());
	}
	
    async delete(resource: URI, opts: FileDeleteOptions): Promise<void> {
		await this.indexedDBFSProvider.deleteKey(resource.path);
    }
    async rename(from: URI, to: URI, opts: FileOverwriteOptions): Promise<void> {
		const value = await this.indexedDBFSProvider.getValue(from.path);
		if (!value) {
			throw new Error(FILE_NOT_FOUND_MSG);
		}
		await this.indexedDBFSProvider.deleteKey(from.path);
		await this.indexedDBFSProvider.setValue(to.path, value);
	}
	
    async readFile(resource: URI): Promise<Uint8Array> {
		const value = await this.indexedDBFSProvider.getValue(resource.path);
		if (!value) {
			throw new Error(FILE_NOT_FOUND_MSG);
		}
		if (value === 'directory') {
			throw new Error(FILE_IS_DIRECTORY_MSG);
		}
		return (typeof TextEncoder !== undefined)
				? new TextEncoder().encode(value)
				: this.encode(value);
    }

	async writeFile(resource: URI, content: Uint8Array, opts: FileWriteOptions): Promise<void> {
		const value = await this.indexedDBFSProvider.getValue(resource.path);
		if (value === 'directory') {
			throw new Error(FILE_IS_DIRECTORY_MSG);
		}

		await this.indexedDBFSProvider.setValue(resource.path, 
			(typeof TextEncoder !== undefined)
			? new TextDecoder().decode(content)
			: this.decode(content));
		this.onDidChangeFileEmitter.fire([{ resource, type: FileChangeType.UPDATED }]);
	}
	
	private encode(value: string): Uint8Array{
		var array = new Uint8Array(value.length);
		for(var i=0; i < value.length; i++ )
			array[i] = value.charCodeAt(i);
		return array;
	}
	
	private decode(buffer: Uint8Array): string{
		var array = Array.from(new Uint8Array(buffer));
		var value = String.fromCharCode.apply(String, array);
		return value;
	}
}