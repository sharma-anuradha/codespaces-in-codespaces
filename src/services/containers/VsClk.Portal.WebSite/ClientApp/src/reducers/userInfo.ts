import {
    getUserInfoSuccessActionType,
    GetUserInfoSuccessAction,
    UserInfo,
} from '../actions/getUserInfo';

type ConfigurationState = UserInfo | null;
type AcceptedActions = GetUserInfoSuccessAction;

export function userInfo(
    state: ConfigurationState | undefined = null,
    action: AcceptedActions
): ConfigurationState {
    switch (action.type) {
        case getUserInfoSuccessActionType:
            return action.payload.userInfo;
        default:
            return state;
    }
}
