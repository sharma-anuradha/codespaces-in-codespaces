import { detect } from 'detect-browser';
import { isHostedOnGithub } from 'vso-client-core';

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
        info.name === 'chrome' || info.name === 'chromium-webview' || info.name === 'edge-chromium' || info.name === 'edge-ios'
    );
}

export function isPartiallySupportedBrowser() {
    if (!info) {
        return false;
    }

    if (info.name === 'edge-ios') {
        return false;
    }

    return isHostedOnGithub() && (info.name === 'safari' || info.os === 'iOS');
}
