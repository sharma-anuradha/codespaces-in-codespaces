declare var AMDLoader: any;

export const configAMD = () => {
    AMDLoader.global.require.config({
        baseUrl: `${window.location.origin}/static/web-standalone/out`,
        paths: {
        'vscode-textmate': `${
            window.location.origin
        }/static/web-standalone/node_modules/vscode-textmate/release/main`,
        'onigasm-umd': `${
            window.location.origin
        }/static/web-standalone/node_modules/onigasm-umd/release/main`,
        xterm: `${window.location.origin}/static/web-standalone/node_modules/xterm/lib/xterm.js`,
        'xterm-addon-search': `${
            window.location.origin
        }/static/web-standalone/node_modules/xterm-addon-search/lib/xterm-addon-search.js`,
        'xterm-addon-web-links': `${
            window.location.origin
        }/static/web-standalone/node_modules/xterm-addon-web-links/lib/xterm-addon-web-links.js`,
        'semver-umd': `${
            window.location.origin
        }/static/web-standalone/node_modules/semver-umd/lib/semver-umd.js`
        }
    });
}
