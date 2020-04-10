import { getVSCodeVersion } from './getVSCodeVersion';

export function getVSCodeAssetPath(
    relativePath: string
) {
    const pathParts = [
        '/web-standalone',
        getVSCodeVersion().commit.substr(0, 7),
        relativePath,
    ];

    return pathParts.join('/').replace(/\/+/g, '/');
}
