import { ICipherPayload } from './ICipherPayload';

export interface ICipherRecord extends ICipherPayload {
    readonly keyId: string;
}
