import { ICodespacePerformanceBlockMeasure } from "../../interfaces/ICodespacePerformanceBlockMeasure";
import { IWaterfallNode } from "../../interfaces/IWaterfallNode";
import { IPerformanceEntry } from "../../interfaces/IPerformanceEntry";
import { CodespacePerformance } from "./CodespacePerformance";

export class WaterfallNode implements IWaterfallNode {
    public readonly groups: IWaterfallNode[] = [];
    public readonly measure: IPerformanceEntry;

    constructor(
        public readonly id: string | number,
        public readonly name: string,
        public readonly path: string,
        public readonly rawMeasure: PerformanceEntry,
        blocks: ICodespacePerformanceBlockMeasure[],
        private readonly perfGoups: CodespacePerformance[],
    ) {
        // measure properties are not
        // iterable, need to copy by hand
        this.measure = {
            name: rawMeasure.name,
            duration: rawMeasure.duration,
            startTime: rawMeasure.startTime,
            entryType: rawMeasure.entryType,
        };

        for (let block of blocks) {
            this.groups.push({
                ...block,
                groups: [],
            });
        }

        for (let group of this.perfGoups) {
            this.groups.push(group.getWaterfall());
        }

        const duration = this.getTotalDuration() - this.measure.startTime;
        this.measure = {
            ...this.measure,
            duration,
        };
    }

    private getTotalDuration = (): number => {
        let maxTime = 0;

        for (let group of this.groups) {
            const totalTime = group.measure.startTime + group.measure.duration;
            if (totalTime > maxTime) {
                maxTime = totalTime;
            }
        }

        return maxTime;
    }
}
