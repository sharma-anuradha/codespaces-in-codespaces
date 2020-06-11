import {
    FetchSecretsAction,
    FetchSecretsSuccessAction,
    FetchSecretsFailureAction,
    fetchSecretsActionType,
    fetchSecretsSuccessActionType,
    fetchSecretsFailureActionType,
} from '../actions/fetchSecrets';
import {
    UpdateSecretAction,
    UpdateSecretSuccessAction,
    UpdateSecretFailureAction,
    updateSecretActionType,
    updateSecretSuccessActionType,
    updateSecretFailureActionType,
} from '../actions/updateSecret';
import { ISecret } from 'vso-client-core';
import {
    CreateSecretAction,
    CreateSecretSuccessAction,
    CreateSecretFailureAction,
    createSecretActionType,
    createSecretSuccessActionType,
    createSecretFailureActionType,
} from '../actions/createSecret';
import {
    DeleteSecretAction,
    DeleteSecretSuccessAction,
    DeleteSecretFailureAction,
    deleteSecretActionType,
    deleteSecretSuccessActionType,
    deleteSecretFailureActionType,
} from '../actions/deleteSecret';

type AcceptedActions =
    | FetchSecretsAction
    | FetchSecretsSuccessAction
    | FetchSecretsFailureAction
    | UpdateSecretAction
    | UpdateSecretSuccessAction
    | UpdateSecretFailureAction
    | CreateSecretAction
    | CreateSecretSuccessAction
    | CreateSecretFailureAction
    | DeleteSecretAction
    | DeleteSecretSuccessAction
    | DeleteSecretFailureAction;

export type SecretsState = {
    secrets: ISecret[];
    isLoadingSecrets: boolean;
    isUpdatingSecret: boolean;
    isCreatingSecret: boolean;
    isDeletingSecret: boolean;
};

export const defaultState: SecretsState = {
    secrets: [],
    isLoadingSecrets: false,
    isUpdatingSecret: false,
    isCreatingSecret: false,
    isDeletingSecret: false,
} as SecretsState;

export function secrets(state: SecretsState = defaultState, action: AcceptedActions): SecretsState {
    switch (action.type) {
        case fetchSecretsActionType:
            return {
                ...state,
                secrets: [],
                isLoadingSecrets: true,
            };
        case fetchSecretsSuccessActionType:
            return {
                ...state,
                secrets: action.payload.secrets,
                isLoadingSecrets: false,
            };
        case fetchSecretsFailureActionType: {
            return {
                ...state,
                isLoadingSecrets: false,
            };
        }
        case updateSecretActionType: {
            return {
                ...state,
                isUpdatingSecret: true,
            };
        }
        case updateSecretSuccessActionType: {
            const secrets: ISecret[] = state.secrets;
            const existingSecretIndex = secrets.findIndex((s) => s.id == action.payload.secret.id);
            if (existingSecretIndex != -1) {
                secrets.splice(existingSecretIndex, 1, action.payload.secret);
            }

            return {
                ...state,
                secrets,
                isUpdatingSecret: false,
            };
        }
        case updateSecretFailureActionType: {
            return {
                ...state,
                isUpdatingSecret: false,
            };
        }
        case createSecretActionType: {
            return {
                ...state,
                isCreatingSecret: true,
            };
        }
        case createSecretSuccessActionType: {
            const secrets: ISecret[] = state.secrets;
            secrets.push(action.payload.secret);
            return {
                ...state,
                secrets,
                isCreatingSecret: false,
            };
        }
        case createSecretFailureActionType: {
            return {
                ...state,
                isCreatingSecret: false,
            };
        }
        case deleteSecretActionType: {
            return {
                ...state,
                isDeletingSecret: true,
            };
        }
        case deleteSecretSuccessActionType: {
            const secrets: ISecret[] = state.secrets;
            const existingSecretIndex = secrets.findIndex((s) => s.id == action.payload.secretId);
            if (existingSecretIndex != -1) {
                secrets.splice(existingSecretIndex, 1);
            }
            return {
                ...state,
                secrets,
                isDeletingSecret: false,
            };
        }
        case deleteSecretFailureActionType: {
            return {
                ...state,
                isDeletingSecret: false,
            };
        }
        default:
            return state;
    }
}
