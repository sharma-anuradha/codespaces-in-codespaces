import { IPerformanceEntry } from './IPerformanceEntry';

export interface IWaterfallNode {
    id: string | number;
    name: string;
    measure: IPerformanceEntry;
    // events: IPerformanceEntry[];
    groups: IWaterfallNode[];
}
