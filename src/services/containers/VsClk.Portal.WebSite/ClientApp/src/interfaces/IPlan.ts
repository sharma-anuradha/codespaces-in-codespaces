import { Locations } from './locations';

export interface IPlan {
    readonly id: string;
    readonly location: Locations;
    readonly name: string;
    readonly resourceGroup: string;
    readonly subscription: string;
}
