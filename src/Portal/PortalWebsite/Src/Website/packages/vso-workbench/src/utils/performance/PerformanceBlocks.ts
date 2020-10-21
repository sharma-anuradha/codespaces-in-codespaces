import { Emitter } from 'vscode-jsonrpc';

import {
    IPerformanceBlock,
    IPerformanceBlockId,
    IPerformanceBlockSide,
} from '../../interfaces/ICodespacePerformance';
import { ICodespacePerformanceBlockMeasure } from '../../interfaces/ICodespacePerformanceBlockMeasure';
import { ICodespacePerformanceBlock } from '../../interfaces/ICodespacePerformanceBlock';

export type TPerformanceMeasureCallbackAsync<T> = (...args: any[]) => Promise<T>;
export type TPerformanceMeasureCallbackSync<T> = (...args: any[]) => T;

/**
 * Utility class to abstract methods around mesuaring performance blocks.
 */
export class PerformanceBlocks {
    private readonly blocks: Record<string, ICodespacePerformanceBlock> = {};

    /**
     * The `pathString` param used to define a full path of a block in
     * the nested blocks tree. Hence the path includes the paths of all
     * parents of the current block in the nested tree.
     */
    constructor(private readonly pathString: string) {}

    /**
     * Mark a `start` or `end` of a code block to measure. Must include
     * a name and can include `id` in case the block measurements need to
     * be retrieved later.
     *
     * Example:
     *
     * ```typescript
     * performanceBlocks.markBlock({ name: 'fetching info', type: 'start' });
     * // .. do some work
     * performanceBlocks.markBlock({ name: 'fetching info', type: 'end' });
     * ```
     * If id is not defined, name for the `start` and `end` should be equal.
     * Each `start` or `end` block with the same `name`/`id` expected to be
     * called once.
     */
    public markBlock = (blockOptions: IPerformanceBlockSide) => {
        const { id, name, type } = blockOptions;

        // get unique block id
        const blockPath = `${this.pathString}.block: ${id ?? name}`;
        // get unique block id for the side (`start`/`end`) of the block
        const pathStringSide = `${blockPath}-${type}`;

        if (this.blocks[blockPath] && this.blocks[blockPath][type]) {
            throw new Error(`The event "${pathStringSide}" already marked in this group.`);
        }

        window.performance.mark(pathStringSide);

        const currentBlock = this.blocks[blockPath] || {
            path: blockPath,
            id: type === 'start' ? id : undefined,
            name: type === 'start' ? name : undefined,
        };
        currentBlock[blockOptions.type] = {
            ...blockOptions,
            path: pathStringSide,
        };

        this.blocks[blockPath] = currentBlock;
        // only elements with id can have event listeners
        if (id) {
            this.invokeBlock({ id, type });
        }

        return this;
    };

    /**
     * Mark the beginning of the block, same as above with `type: 'start'`
     */
    public markBlockStart = (blockOptions: IPerformanceBlock) => {
        return this.markBlock({
            type: 'start',
            ...blockOptions,
        });
    };

    /**
     * Mark the end of the block, same as above with `type: 'end'`
     */
    public markBlockEnd = (blockOptions: IPerformanceBlock) => {
        return this.markBlock({
            type: 'end',
            ...blockOptions,
        });
    };

    /**
     * Convenience method around the `markBlock` since that later has to be
     * called twice - once for `start` and once for `end`, we created this
     * wrapper method that does that for the user.
     */
    public measure = async <T>(
        options: IPerformanceBlock,
        callback: TPerformanceMeasureCallbackAsync<T>
    ) => {
        this.markBlock({
            ...options,
            type: 'start',
        });

        try {
            return await callback();
        } finally {
            this.markBlock({
                ...options,
                type: 'end',
            });
        }
    };

    /**
     * Sync version of the `measure`.
     */
    public measureSync = <T>(
        options: IPerformanceBlock,
        callback: TPerformanceMeasureCallbackSync<T>,
    ) => {
        this.markBlock({
            ...options,
            type: 'start',
        });

        try {
            return callback();
        } finally {
            this.markBlock({
                ...options,
                type: 'end',
            });
        }
    };

    /**
     * Method to calculate(measure) all the currently tracked timing blocks.
     */
    public measures = (): ICodespacePerformanceBlockMeasure[] => {
        const blocks: ICodespacePerformanceBlockMeasure[] = [];
        for (let [id, block] of Object.entries(this.blocks)) {
            const { start, end } = block;
            if (!start || !end) {
                continue;
            }

            window.performance.measure(block.path, `${start.path}`, `${end.path}`);

            const [measure] = window.performance.getEntriesByName(block.path);
            if (!measure) {
                throw new Error(`No measure found in Performance API for "${id}".`);
            }

            blocks.push({
                id: block.id,
                name: block.name,
                measure,
            });
        }

        return blocks;
    };

    private static readonly onEventEmitters: Record<string, Emitter<IPerformanceBlockId>> = {};

    private invokeBlock = (options: IPerformanceBlockId) => {
        const blockCallbackId = this.getBlockCallbackId(options);

        const emitter = PerformanceBlocks.onEventEmitters[blockCallbackId];
        if (!emitter) {
            return;
        }

        emitter.fire(options);
    };

    private getBlockCallbackId = (options: IPerformanceBlockId) => {
        const { id, type } = options;
        return `${id}-${type}`;
    };

    /**
     * Method to define event listeners for the `start` or `end` block sides.
     */
    public onEvent = (
        options: IPerformanceBlockId,
        callback: (options: IPerformanceBlockId) => any
    ) => {
        const blockCallbackId = this.getBlockCallbackId(options);

        let emitter = PerformanceBlocks.onEventEmitters[blockCallbackId];
        if (!emitter) {
            emitter = new Emitter();
            PerformanceBlocks.onEventEmitters[blockCallbackId] = emitter;
        }

        emitter.event(callback);

        return this;
    };
}
