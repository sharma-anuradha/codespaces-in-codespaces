import { PlatformQueryParams } from '../constants';

/**
 * Parse `autoStart` query param, fallback to `true`.
 */
const getAutoStartQueryParam = (): boolean => {
    const params = new URLSearchParams(location.search);
    const param = params.get(PlatformQueryParams.AutoStart) || '';

    return param.toLocaleLowerCase() !== 'false';
}

/**
 * Parse `autoConnect` query param, fallback to `true`.
 */
const getAutoAuthorizeQueryParam = (): boolean => {
    const params = new URLSearchParams(location.search);
    const param = params.get(PlatformQueryParams.AutoAuthorize) || '';

    return param.toLocaleLowerCase() !== 'false';
}

export async function getPlatformQueryParam(paramName: PlatformQueryParams.AutoStart): Promise<boolean>;
export async function getPlatformQueryParam(paramName: PlatformQueryParams.AutoAuthorize): Promise<boolean>;
export async function getPlatformQueryParam(paramName: PlatformQueryParams): Promise<string | boolean | number | null> {
    switch (paramName) {
        case PlatformQueryParams.AutoStart: {
            return getAutoStartQueryParam();
        }

        case PlatformQueryParams.AutoAuthorize: {
            return getAutoAuthorizeQueryParam();
        }

        default: {
            throw new Error(`Unknown query param name "${paramName}".`);
        }
    }
}
