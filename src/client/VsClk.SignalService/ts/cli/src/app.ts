import {
    HubClient,
    ContactServiceProxy,
    RelayServiceProxy,
    IRelayHubProxy,
    SendOption
} from '@vs/vso-signalr-client';

import { RpcMessageStream, RelayDataChannel } from '@vs/vso-signalr-client-jsonrpc';

import * as signalR from '@microsoft/signalr';
import * as process from 'process';
import * as yargs from 'yargs';
import * as readline from 'readline';
import * as rpc from 'vscode-jsonrpc';

import { StringDecoder } from 'string_decoder';
const decoder = new StringDecoder('utf8');

const argv = yargs
    .option('id', {
        description: 'contact Id',
        type: 'string',
    })
    .option('token', {
        description: 'auth token',
        alias: 't',
        type: 'string'
    })
    .option('service', {
        alias: 's',
        description: 'Service Uri',
        type: 'string',
    })
    .option('relayHub', {
        alias: 'r',
        description: 'If releay hub app',
        type: 'boolean',
    }) 
    .demandOption('id')
    .help()
    .alias('help', 'h')
    .argv;

async function main() {

    const serviceUri = argv.service || 'http://localhost:5000/signalrhub';

    const logger: signalR.ILogger = {
        log: (level: signalR.LogLevel, msg: string) => console.log(msg)
    };

    const httpConnectionOptions: signalR.IHttpConnectionOptions = { logger: undefined };
    if (argv.token) {
        httpConnectionOptions.accessTokenFactory = () => argv.token!;
    }

    const hubClient = HubClient.create(serviceUri, httpConnectionOptions);

    const keyPressCallback = argv.relayHub ? main_relay(hubClient, logger) : main_presence(hubClient, logger);

    await hubClient.start();
    console.log('connected...');

    readline.emitKeypressEvents(process.stdin);
    process.stdin.setRawMode!(true);

    process.stdin.on('keypress', async (str: string, key: any) => {
        if (key.name === 'q') {
            process.exit();
        } else {
            try {
                await keyPressCallback(key.name);
            } catch (err) {
                console.log(`Error: ${err}`);
            }
        }
    });
}

function main_presence(hubClient: HubClient, logger: signalR.ILogger): (key: any) => Promise<void> {  
    const contactServiceProxy = new ContactServiceProxy(hubClient.hubProxy, logger, true );

    contactServiceProxy.onUpdateProperties((contact, properties, targetConnectionId) => {
        // our ILogger will already send this to the console
    });

    contactServiceProxy.onMessageReceived((targetContact, fromContact, messageType, body) => {
        // our ILogger will already send this to the console
    });

    hubClient.onConnectionStateChanged(async () => {
        console.log(`onConnectionStateChanged-> state:${hubClient.state}`);

        if (hubClient.state === signalR.HubConnectionState.Connected && (argv.token || argv.id)) {

            const initialProperties = {
                'status': 'available'
            };

            const registerInfo = await contactServiceProxy.registerSelfContact(argv.id, initialProperties);
            console.log(`registerInfo-> ${JSON.stringify(registerInfo)}`);
        }
    });

    return async (key: any): Promise<void> => {
        if (key === 'a') {
            await contactServiceProxy.publishProperties({
                'status': 'available'
            });
        } else if (key === 'b') {
            await contactServiceProxy.publishProperties({
                'status': 'busy'
            });
        } else if (key === 's') {
            const response = await contactServiceProxy.requestSubcriptions([ { 'email': 'rcollado@gmail.com' }
            ], ['*'], true);
            console.log(`requestSubcriptions-> ${JSON.stringify(response)}`);
        } else if (key === 'x') {
            await contactServiceProxy.sendMessage({'id': 'rcollado'}, 'typeTest', { 'Text': 'hi from node', 'Metadata': { 'Type': 'text' } });
        }
    };
}

function main_relay(hubClient: HubClient, logger: signalR.ILogger): (key: any) => Promise<void> {  
    const relayServiceProxy = new RelayServiceProxy(hubClient.hubProxy, logger, true);

    let rpcConnection: rpc.MessageConnection;
    let relayHubProxy: IRelayHubProxy;
    return async (key: any): Promise<void> => {
        if (key === 'j') {
            relayHubProxy = await relayServiceProxy.joinHub('test', { 'app': 'node', 'userId': 'none'}, true);
            console.log(`Joined-> serviceId:${relayHubProxy.serviceId} stamp:${relayHubProxy.stamp} Participants: ${JSON.stringify(relayHubProxy.participants)}`);
            relayHubProxy.onReceiveData((receivedData): Promise<void> =>  {
                if (receivedData.type === 'test') {
                    const buf = Buffer.from(receivedData.data);
                    const msg = decoder.write(buf);
                    console.log(`received message:${msg}`);
                }
                return Promise.resolve();
            });
        } else if (key === 's') {
            if (!relayHubProxy) {
                console.log(`you need to join to a hub first`);
            } else {
                const buf: Uint8Array = Buffer.from('Hi from node', 'utf8');
                await relayHubProxy.sendData(SendOption.None, null, 'test', buf);
            }
        } else if (key === 'd') {
            if (!relayHubProxy) {
                console.log(`you need to join to a hub first`);
            } else {
                await relayServiceProxy.deleteHub(relayHubProxy.id);
            }
        } else if (key === 'r') {
            if (!relayHubProxy) {
                console.log(`you need to join to a hub first`);
            } else {
                const participants = relayHubProxy.participants.filter(p => !p.isSelf);
                if (participants.length > 0) {
                    const relayDataChannel = new RelayDataChannel(relayHubProxy, 'jsonRpc', participants[0].id);
                    const rpcMessageStream = new RpcMessageStream(relayDataChannel);

                    rpcConnection = rpc.createMessageConnection(
                        rpcMessageStream.reader,
                        rpcMessageStream.writer);
            
                    rpcConnection.onNotification('notify1', (value1) => {
                        console.log(`notify1 received:${value1}`);
                    });
                    rpcConnection.listen();
                    console.log(`json rpc started on participant:${participants[0].id}`);
                }
            }
        } else if (key === 'm') {
            if (!rpcConnection) {
                console.log(`mo rpc setup`);
            } else {
                const result = await rpcConnection.sendRequest<string>('method1', 10, 'hi from typescript');
                console.log(`json rpc result:${result}`);
            }
        }
    };
}

main()
    .then(() => {
        console.log('succeeded..');
    })
    .catch(err => {
        // Deal with the fact the chain failed
    });