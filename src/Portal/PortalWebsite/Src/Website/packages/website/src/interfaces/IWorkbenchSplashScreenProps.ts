import { ILocalEnvironment } from 'vso-client-core';

export interface IWorkbenchSplashScreenProps {
    showPrompt: boolean;
    environment: ILocalEnvironment;
    connectError?: string | null;
    onRetry?: () => void;
    onConnect: () => void;
}
