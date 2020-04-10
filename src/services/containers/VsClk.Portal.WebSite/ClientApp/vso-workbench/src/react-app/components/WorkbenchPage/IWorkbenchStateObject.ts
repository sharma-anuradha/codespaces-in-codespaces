import { IEnvironment } from 'vso-client-core';
import { TEnvironmentState } from '../../../interfaces/TEnvironmentState';

export interface IWorkbenchStateObject {
    value: TEnvironmentState;
    environmentInfo?: IEnvironment;
    message?: string;
}
