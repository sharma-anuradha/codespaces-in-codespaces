import { IPerformanceEntry } from './IPerformanceEntry';

export interface ICodespacePerformanceBlockMeasure {
    id: string;
    name: string;
    measure: IPerformanceEntry;
}
