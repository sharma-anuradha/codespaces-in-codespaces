import { ILocalCloudEnvironment } from './cloudenvironment';

export enum SplashScreenType {
    Creation,
    Starting,
}

export interface IWorkbenchSplashScreenProps {
    screentype: SplashScreenType;
    showPrompt: boolean;
    environment: ILocalCloudEnvironment;
    connectError?: string | null;
    onRetry?: () => void;
    onConnect: () => void;
}
