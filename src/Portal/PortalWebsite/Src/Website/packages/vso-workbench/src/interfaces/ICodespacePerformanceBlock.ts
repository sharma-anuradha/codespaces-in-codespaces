import { IPerformanceBlockSideRecord } from './ICodespacePerformance';

export interface ICodespacePerformanceBlock {
    id: string;
    name: string;
    path: string;
    start?: IPerformanceBlockSideRecord;
    end?: IPerformanceBlockSideRecord;
}
