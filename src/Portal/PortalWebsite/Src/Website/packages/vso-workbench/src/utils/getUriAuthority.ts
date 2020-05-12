import { IEnvironment } from 'vso-client-core';

export const getUriAuthority = (environmentInfo: IEnvironment) => {
    return `vsonline+${environmentInfo.id}`;
};
