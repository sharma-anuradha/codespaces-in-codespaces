import { Signal } from '../utils/Signal';
import { randomString } from '../utils/randomString';
import { isKnownPartnerTLD } from "../utils/isKnownPartnerTLD";
import { IPartnerInfo } from '../interfaces/IPartnerInfo';
import { validatePartnerInfo } from './validatePartnerInfo';
import { TPostMessageChannelMessages } from '../interfaces/TPostMessageChannelMessages';
import { createTrace } from '../utils/createTrace';

const trace = createTrace('vso-client-core:postMessageChannel');

type TPostMessageAuthResult = 'error' | 'success';

export class PostMessageChannel {
    private signals = new Map<string, Signal<IPartnerInfo>>();

    constructor() {
        self.addEventListener('message', this.receiveMessage, false);
    }

    public getRepoInfo = async (id = randomString()): Promise<IPartnerInfo> => {
        const signal = new Signal<IPartnerInfo>();
        this.signals.set(id, signal);

        self.parent.postMessage(
            {
                type: TPostMessageChannelMessages.GetPartnerInfo,
                id,
            },
            document.referrer
        );
        
        const timeout = 5 * 1000;
        const timer = setTimeout(() => {
            this.signals.delete(id);
            this.reportResult('error', `Embedder did not respond in ${timeout}ms.`);
            signal.cancel();
        }, timeout);

        signal.promise.finally(() => {
            clearTimeout(timer);
        });

        const data = await signal.promise;

        return data;
    };

    public reportResult = (result: TPostMessageAuthResult, message: string) => {
        self.parent.postMessage(
            {
                type: TPostMessageChannelMessages.GetPartnerInfoResult,
                id: randomString(),
                result,
                message,
            },
            document.referrer
        );

        trace.info(`Reporting result "${result}" to the partner.`);
    };

    private receiveMessage = (e: MessageEvent) => {
        // ignore messages from unknown partners
        if (!isKnownPartnerTLD(e.origin)) {
            this.reportResult('error', `Unknown embedder domain ${e.origin}.`);
            return;
        }

        const { responseId, type } = e.data as IPartnerInfo;
        if (type !== TPostMessageChannelMessages.GetPartnerInfoResponse) {
            this.reportResult('error', `Unexpected embedder response message type ${type}.`);
            return;
        }

        if (!responseId) {
            this.reportResult('error', `No responseId set.`);
            throw new Error(`Received a message from parent but "responseId" is not set.`);
        }

        const signal = this.signals.get(responseId);
        if (!signal) {
            this.reportResult('error', `Unknown responseId.`);
            return;
        }

        const { data } = e;
        validatePartnerInfo(data);

        this.signals.delete(responseId);
        signal.complete(data);
    }
};
