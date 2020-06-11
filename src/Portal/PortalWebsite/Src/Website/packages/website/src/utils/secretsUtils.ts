import { useState } from 'react';
import { TFunction } from 'i18next';

import { FilterType, SecretErrorCodes } from 'vso-client-core';

export const getFilterDisplayName = (filterType: FilterType, translation: TFunction): string => {
    switch (filterType) {
        case FilterType.GitRepo: {
            return translation('gitRepository');
        }
        case FilterType.CodespaceName: {
            return translation('codespaceName');
        }
        default: {
            return filterType;
        }
    }
};

/**
 * A hook that provides onChange event binding and manages the state for backing a fluentui TextField.
 * State can be validated, read, set, reset to initial value, and tracked for modifications.
 * @param initialValue Optional initial value to be set for the text field.
 */
export const useTextInput = (initialValue?: string | undefined) => {
    const [value, setValue] = useState<string | undefined>(initialValue);
    const [isModified, setIsModified] = useState(false);
    const [isValid, setIsValidState] = useState<boolean>();

    return {
        value,
        setValue,
        isModified,
        setIsModified,
        isValid,
        setIsValid(valid: boolean) {
            setIsValidState(valid);
        },
        resetInput() {
            setValue(initialValue);
            setIsModified(false);
            setIsValidState(false);
        },
        bind: {
            value,
            onChange(
                _event: React.FormEvent<HTMLInputElement | HTMLTextAreaElement>,
                newValue?: string
            ) {
                setValue(newValue);
                setIsModified(true);
            },
        },
    };
};

/**
 * Generic state hook that can be validated, read, set, reset to initial value, and tracked for modifications.
 * @param initialState Optional initial state of generic type.
 */
export const useValidatableState = <S>(initialState: S) => {
    const [value, setValue] = useState<S>(initialState);
    const [isModified, setIsModified] = useState(false);
    const [isValid, setIsValidState] = useState(false);

    return {
        value,
        setState: (newValue: S) => {
            setValue(newValue);
            setIsModified(true);
        },
        isModified,
        isValid,
        setIsValid(valid: boolean) {
            setIsValidState(valid);
        },
        resetState() {
            setValue(initialState);
            setIsModified(false);
            setIsValidState(false);
        },
    };
};

const errors = {
    [SecretErrorCodes.ExceededSecretsQuota]: 'secretErrorExceededQuota',
    [SecretErrorCodes.FailedToCreateSecret]: 'secretErrorFailedToCreate',
    [SecretErrorCodes.FailedToCreateSecretStore]: 'secretErrorFailedToCreate',
    [SecretErrorCodes.FailedToDeleteSecret]: 'secretErrorFailedToDelete',
    [SecretErrorCodes.FailedToUpdateSecret]: 'secretErrorFailedToUpdate',
    [SecretErrorCodes.NotReady]: 'secretErrorNotReady',
    [SecretErrorCodes.SecretNotFound]: 'secretErrorNotFound',
    [SecretErrorCodes.UnauthorizedScope]: 'secretErrorUnauthroizedScope',
    [SecretErrorCodes.Unknown]: 'secretErrorUnknown',
};

export const secretErrorCodeToString = (code: SecretErrorCodes, translation: TFunction): string =>
    translation(errors[code] ?? errors[SecretErrorCodes.Unknown]);
