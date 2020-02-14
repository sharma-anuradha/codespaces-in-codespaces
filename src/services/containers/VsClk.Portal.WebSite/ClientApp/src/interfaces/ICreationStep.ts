export interface ICreationStep {
    name: ICreationStepName;
    status: 'waiting' | 'locating' | 'found' | 'running' | 'skipped' | 'completed' | 'failed' | 'finalizing';
}

export type ICreationStepName = 'configuration' | 'containerSetup' | 'environmentCreated'
