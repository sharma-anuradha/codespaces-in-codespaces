export enum SecretType {
    EnvironmentVariable = 'EnvironmentVariable',
}

export enum FilterType {
    GitRepo = 'GitRepo',
    CodespaceName = 'CodespaceName',
}

export enum SecretScope {
    Plan = 'Plan',
    User = 'User',
}

export type SecretAction = 'Create' | 'Update' | 'Delete';

export interface ISecretFilter {
    type: FilterType;
    value: string;
}

export interface ISecret {
    readonly scope: SecretScope;
    readonly id: string;
    readonly lastModified: Date;
    readonly secretName: string;
    readonly value?: string;
    readonly type: SecretType;
    readonly filters?: ISecretFilter[];
    readonly notes?: string;
}

export interface ICreateSecretRequest {
    scope: SecretScope;
    secretName: string;
    value: string;
    type: SecretType;
    filters?: ISecretFilter[];
    notes?: string;
}

export interface IUpdateSecretRequest {
    scope: SecretScope;
    secretName?: string;
    value?: string;
    filters?: ISecretFilter[];
    notes?: string;
}

export enum SecretErrorCodes {
    Unknown = 0,
    NotReady = 10,
    FailedToCreateSecretStore = 20,
    FailedToCreateSecret = 30,
    UnauthorizedScope = 40,
    FailedToUpdateSecret = 50,
    FailedToDeleteSecret = 60,
    SecretNotFound = 80,
    ExceededSecretsQuota = 90,
}
