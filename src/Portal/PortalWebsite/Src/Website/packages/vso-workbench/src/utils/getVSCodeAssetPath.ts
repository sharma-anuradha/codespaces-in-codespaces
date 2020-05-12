import { getVSCodeVersionString } from './getVSCodeVersion';

export function getVSCodeAssetPath(relativePath: string) {
    const pathParts = [
        '/workbench-page/web-standalone',
        `${getVSCodeVersionString()}`,
        relativePath,
    ];

    return pathParts.join('/').replace(/\/+/g, '/');
}
