import { CodespacePerformance } from '../../../utils/performance/CodespacePerformance';

/**
 * Decorator method to measure `start`/`end` times of entire method. Expects
 * `performance` property set to the isntance of `CodespacePerformance` on
 * the method's class. Covenience over the `CodespacePerformance.markBlock` method.
 */

export function performanceAsync(perfBlockName: string, groupId?: string | number) {
    return function (
        target: any,
        propertyName: string,
        descriptor: TypedPropertyDescriptor<(...args: any[]) => any>
    ) {
        const method = descriptor.value;

        descriptor.value = async function performance() {
            const perf = (this as any).performance as CodespacePerformance | undefined;
            if (!perf) {
                throw new Error(
                    'No `this.performance` set.'
                );
            }

            await perf.measure(
                {
                    name: perfBlockName,
                },
                async () => {
                    return await method!.apply(this, [...arguments]);
                },
            );
        };
    };
}
