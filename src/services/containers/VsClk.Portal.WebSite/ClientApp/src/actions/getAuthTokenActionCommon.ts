
type TGetAuthTokenAction = () => Promise<string>;
let authTokenAction: TGetAuthTokenAction | undefined;

export const getAuthTokenAction = ():  TGetAuthTokenAction => {
    if (!authTokenAction) {
        throw new Error('No `authTokenAction` set, call `setCommonAuthTokenAction` first.');
    }

    return authTokenAction;
};

export const setCommonAuthTokenAction = (action: TGetAuthTokenAction) => {
    authTokenAction = action;
};
