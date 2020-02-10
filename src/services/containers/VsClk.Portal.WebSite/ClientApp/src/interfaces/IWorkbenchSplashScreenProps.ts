import { ILocalCloudEnvironment } from './cloudenvironment';

export interface IWorkbenchSplashScreenProps {
    showPrompt: boolean;
    environment: ILocalCloudEnvironment;
    connectError: string | null;
    onRetry: () => void;
    onConnect: () => void;
}
