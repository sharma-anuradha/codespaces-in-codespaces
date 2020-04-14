import { getVSCodeVersion } from './getVSCodeVersion';

export function getVSCodeAssetPath(
    relativePath: string
) {
    const version = getVSCodeVersion();
    
    const pathParts = [
        '/workbench-page/web-standalone',
        `${version.quality}-${version.commit.substr(0, 7)}`,
        relativePath,
    ];

    return pathParts.join('/').replace(/\/+/g, '/');
}
