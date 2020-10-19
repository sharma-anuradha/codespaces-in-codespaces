import { PlatformQueryParams } from '../constants';

export async function getQueryParamFlag(paramName: PlatformQueryParams.AutoStart | PlatformQueryParams.AutoAuthRedirect): Promise<boolean>;
export async function getQueryParamFlag(paramName: PlatformQueryParams): Promise<string | boolean | number | null> {
    const params = new URLSearchParams(location.search);
    const param = params.get(paramName) || '';

    return param.toLowerCase() !== 'false';
}

export async function setQueryParamFlag(paramName: PlatformQueryParams.AutoStart | PlatformQueryParams.AutoAuthRedirect, flag: boolean): Promise<void>;
export async function setQueryParamFlag(paramName: PlatformQueryParams, flag: string | boolean | number | null): Promise<void> {
    const currentUrl = new URL(window.location.href);
    currentUrl.searchParams.set(paramName, `${flag}`);

    window.location.replace(currentUrl.toString());
}
