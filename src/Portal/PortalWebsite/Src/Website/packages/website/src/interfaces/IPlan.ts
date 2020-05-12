import { Locations } from './ILocation';

export interface IPlan {
    readonly id: string;
    readonly location: Locations;
    readonly name: string;
    readonly resourceGroup: string;
    readonly subscription: string;
}
