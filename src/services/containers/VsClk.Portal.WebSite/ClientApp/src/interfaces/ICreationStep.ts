export interface ICreationStep {
    name: ICreationStepName;
    status: 'Pending' | 'Running' | 'Succeeded' | 'Failed';
}

export type ICreationStepName = 'initializeEnvironment' | 'buildContainer' | 'runContainer'
