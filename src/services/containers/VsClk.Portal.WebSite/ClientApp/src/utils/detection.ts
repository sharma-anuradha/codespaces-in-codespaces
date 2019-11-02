import { detect } from 'detect-browser';

const info = detect();

export function isMacOs() {
    if (!info) {
        return false;
    }
    return info.os === 'Mac OS';
}

export function isSupportedBrowser() {
    if (!info) {
        return false;
    }

    return (
        info.name === 'chrome' || info.name === 'chromium-webview' || info.name === 'edge-chromium'
    );
}
