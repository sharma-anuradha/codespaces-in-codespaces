import { TKnownPartners } from './TKnownPartners';

export interface ICascadeToken {
    idp: TKnownPartners;
    exp: number;
}
