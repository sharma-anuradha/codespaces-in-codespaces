import {
    IPerformanceBlockSide,
    IPerformanceBlock,
    IPerformanceBlockId,
} from '../../interfaces/ICodespacePerformance';
import { IWaterfallNode } from '../../interfaces/IWaterfallNode';
import { WaterfallNode } from './WaterfallNode';
import { PerformanceEventIds } from './PerformanceEvents';
import {
    PerformanceBlocks,
    TPerformanceMeasureCallbackAsync,
    TPerformanceMeasureCallbackSync,
} from './PerformanceBlocks';

/**
 * Main performance measurement class. Meant do be used recusively,
 * e.g. every `createGroup` mathod call will return the instance of
 * the same class which is linked to the parent. This allows to create
 * callstack-like performance trees or waterfalls.
 */
export class CodespacePerformance {
    private static groupCount = 0;
    private readonly blocks: PerformanceBlocks;
    private readonly groups: CodespacePerformance[] = [];

    constructor(
        public readonly name: string,
        public readonly groupId: string | number,
        public readonly pathString: string = 'start'
    ) {
        this.blocks = new PerformanceBlocks(pathString);

        window.performance.mark(pathString);
    }

    /**
     * Create the child subgroup of the current group.
     */
    public createGroup = (
        groupName: string,
        groupdId: string | number = CodespacePerformance.groupCount++
    ) => {
        const pathString = `${this.pathString}.${groupName}.${this.groupId}`;

        const newGroup = new CodespacePerformance(groupName, groupdId, pathString);
        this.groups.push(newGroup);

        return newGroup;
    };

    /**
     * Marks `start` or `end` point of a code block that need to be measured
     * in between. see `blocks.markBlock` method note for man example.
     */
    public markBlock = (blockOptions: IPerformanceBlockSide) => {
        this.blocks.markBlock(blockOptions);

        return this;
    };

    /**
     * Marks `start` point of a code block that need to be measured
     * in between. see `blocks.markBlock` method note for man example.
     */
    public markBlockStart = (blockOptions: IPerformanceBlock) => {
        this.blocks.markBlockStart(blockOptions);

        return this;
    };

    /**
     * Marks `end` point of a code block that need to be measured
     * in between. see `blocks.markBlock` method note for man example.
     */
    public markBlockEnd = (blockOptions: IPerformanceBlock) => {
        this.blocks.markBlockEnd(blockOptions);

        return this;
    };

    /**
     * Convenience method for the `markBlock` method. Instead of calling
     * the `markBlock` twice, the `measure` can wrap the code block of interest
     * into an asynchronous function hence be called only once.
     */
    public measure = async <T>(
        options: IPerformanceBlock,
        callback: TPerformanceMeasureCallbackAsync<T>
    ) => {
        return await this.blocks.measure(options, callback);
    };

    /**
     * Sync version of the `measure`.
     */
    public measureSync = <T>(
        options: IPerformanceBlock,
        callback: TPerformanceMeasureCallbackSync<T>,
    ) => {
        return this.blocks.measureSync(options, callback);
    };

    /**
     * Get waterfall of all the nested performance blocks, starting from the main
     * "start" root node that is initialized as singleton when app starts.
     */
    public getWaterfall = (): IWaterfallNode => {
        window.performance.measure(this.pathString, this.pathString);
        const [measure] = window.performance.getEntriesByName(this.pathString);

        const node = new WaterfallNode(
            this.groupId,
            this.name,
            this.pathString,
            measure,
            this.blocks.measures(),
            this.groups
        );

        return node;
    };

    /**
     * Get a measurement of a specific block by `id`.
     */
    public getBlock = (id: string | number, waterfall?: IWaterfallNode): IWaterfallNode | null => {
        waterfall = waterfall ?? getMainCodespacePerformance().getWaterfall();

        if (waterfall.id === id) {
            return waterfall;
        }

        for (let group of waterfall.groups) {
            const result = this.getBlock(id, group);
            if (result) {
                return result;
            }
        }

        return null;
    };

    /**
     * Subscribe to `start` or `end` events of a block by `id`.
     */
    public onEvent(options: IPerformanceBlockId, callback: (block?: IWaterfallNode) => any) {
        this.blocks.onEvent(options, (opts: IPerformanceBlockId) => {
            const { id, type } = opts;

            if (type === 'start') {
                callback();
                return this;
            }

            const node = this.getBlock(id);
            if (!node) {
                throw new Error(`Cannot find "${id}" block.`);
            }

            callback(node);
        });

        return this;
    }

    /**
     * Get a block by `id` from the main performance singleton class.
     */
    public static getBlockFromMain = (id: string | number): IWaterfallNode | null => {
        const performance = getMainCodespacePerformance();
        const block = performance.getBlock(id);

        return block;
    };

    /**
     * Get block `startTime` by block `id` or `null` if block is not found.
     */
    public static getBlockStartTime = (id: string | number): number | null => {
        const block = CodespacePerformance.getBlockFromMain(id);
        if (!block) {
            return null;
        }

        const { measure } = block;
        return measure.startTime;
    };

    /**
     * Get block `endTime` by block `id` or `null` if block is not found.
     */
    public static getBlockEndTime = (id: string | number): number | null => {
        const block = CodespacePerformance.getBlockFromMain(id);

        if (!block) {
            return null;
        }

        const { measure } = block;
        return measure.startTime + measure.duration;
    };

    /**
     * Get block `durationTime` by block `id` or `null` if block is not found.
     */
    public static getBlockDurationTime = (id: string | number): number | null => {
        const block = CodespacePerformance.getBlockFromMain(id);

        if (!block) {
            return null;
        }

        const { measure } = block;
        return measure.duration;
    };
}

/**
 * Function to initialize the main performance singleton class, meant to be
 * called on the app startup.
 */
let codespacePerformance: CodespacePerformance | null = null;
export const initializeCodespacePerformanceInstance = () => {
    codespacePerformance = new CodespacePerformance('start', PerformanceEventIds.Start);

    return codespacePerformance;
};

/**
 * Function get the main performance singleton class
 * from this module, or throw if not yet initialized
 */
export const getMainCodespacePerformance = () => {
    if (!codespacePerformance) {
        throw new Error('No CodespacePerformance instance found, please initialize first.');
    }

    return codespacePerformance;
};
