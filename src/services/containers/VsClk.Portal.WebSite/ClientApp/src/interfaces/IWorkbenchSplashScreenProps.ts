import { ReactEventHandler } from 'react';
import { ILocalCloudEnvironment } from './cloudenvironment';

export interface IWorkbenchSplashScreenProps {
    environment: ILocalCloudEnvironment;
    connectError: string | null;
    onRetry: () => void;
}
