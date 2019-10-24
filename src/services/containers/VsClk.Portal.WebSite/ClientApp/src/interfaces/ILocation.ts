import { ISku } from './ISku';

export interface ILocations {
    readonly current: string;
    readonly available: Locations[];
}

export interface ILocation {
    readonly skus: ISku[];
}

export type Locations = 'WestUs2' | 'EastUs' | 'SouthEastAsia' | 'WestEurope';
