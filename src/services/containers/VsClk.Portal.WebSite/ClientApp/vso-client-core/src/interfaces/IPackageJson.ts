export interface IPackageJson {
    readonly version: string;
    readonly name: string;
    readonly vscodeCommit: {
        readonly insider: string;
        readonly stable: string
    };
}
