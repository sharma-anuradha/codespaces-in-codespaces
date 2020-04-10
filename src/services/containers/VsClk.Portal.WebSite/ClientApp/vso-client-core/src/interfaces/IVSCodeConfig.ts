import { TVSCodeQuality } from './TVSCodeQuality';

export interface IVSCodeConfig {
    readonly commit: string;
    readonly quality: TVSCodeQuality;
}
