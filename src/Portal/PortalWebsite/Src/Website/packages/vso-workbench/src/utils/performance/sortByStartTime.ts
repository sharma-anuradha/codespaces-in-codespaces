import { IPerformanceEntry } from '../../interfaces/IPerformanceEntry';

export const sortByStartTime = <T extends { measure: IPerformanceEntry }>(items: T[]): T[] => {
    const result = [...items].sort((item1, item2) => {
        return item1.measure.startTime - item2.measure.startTime;
    });

    return result;
};