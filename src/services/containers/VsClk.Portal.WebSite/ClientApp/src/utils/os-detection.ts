import { detect } from 'detect-browser';

const info = detect();

export function isMacOs() {
    if (!info) {
        return false;
    }
    return info.os === 'Mac OS';
}
