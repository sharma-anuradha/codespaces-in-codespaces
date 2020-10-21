export interface IPerformanceBlock {
    id?: string | number;
    name: string;
}

export interface IPerformanceBlockSide extends IPerformanceBlock {
    type: 'start' | 'end';
}

export interface IPerformanceBlockSideRecord extends IPerformanceBlockSide {
    path: string;
}

export interface IPerformanceBlockId {
    id: string | number;
    type: 'start' | 'end';
}
