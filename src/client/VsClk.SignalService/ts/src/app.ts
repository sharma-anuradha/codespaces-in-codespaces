import { HubClient } from './HubClient';
import { PresenceServiceProxy } from './PresenceServiceProxy';
import { RelayServiceProxy } from './RelayServiceProxy';

import * as signalR from '@aspnet/signalr';
import * as process from 'process';
import * as yargs from 'yargs';
import * as readline from 'readline';
import { IRelayHubProxy } from './IRelayServiceProxy';

import {StringDecoder} from 'string_decoder';
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
    .option('useSignalRHub', {
        alias: 'u',
        description: 'If using universal signalR hub',
        type: 'boolean',
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

    const serviceUri = argv.service || argv.useSignalRHub ? 'http://localhost:5000/signalrhub' : 'http://localhost:5000/presencehub';

    const logger: signalR.ILogger = {
        log: (level: signalR.LogLevel, msg: string) => console.log(msg)
    };

    const hubClient = argv.token ? HubClient.createWithUrlAndToken(serviceUri, () => argv.token!, logger) : HubClient.createWithUrl(serviceUri, logger);

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
            }
            catch(err) {
                console.log(`Error: ${err}`);
            }
        }
    });
}

function main_presence(hubClient: HubClient,logger: signalR.ILogger): (key: any) => Promise<void> {  
    const presenceServiceProxy = new PresenceServiceProxy(hubClient.hubConnection, logger, argv.useSignalRHub );

    presenceServiceProxy.onUpdateProperties((contact, properties, targetConnectionId) => {
        // our ILogger will already send this to the console
    });

    hubClient.onConnectionStateChanged(async () => {
        console.log(`onConnectionStateChanged-> state:${hubClient.state}`);

        if (hubClient.state === signalR.HubConnectionState.Connected && (argv.token || argv.id)) {

            const initialProperties = {
                'status': 'available'
            };

            const registerInfo = await presenceServiceProxy.registerSelfContact(argv.id, initialProperties);
            console.log(`registerInfo-> ${JSON.stringify(registerInfo)}`);
        }
    });

    return async (key: any): Promise<void> => {
        if (key === 'a') {
            await presenceServiceProxy.publishProperties({
                'status': 'available'
            });
        } else if (key === 'b') {
            await presenceServiceProxy.publishProperties({
                'status': 'busy'
            });
        }
    };
}

function main_relay(hubClient: HubClient,logger: signalR.ILogger): (key: any) => Promise<void> {  
    const relayServiceProxy = new RelayServiceProxy(hubClient.hubConnection, logger, argv.useSignalRHub);

    let relayHubProxy: IRelayHubProxy;
    return async (key: any): Promise<void> => {
        if (key === 'j') {
            relayHubProxy = await relayServiceProxy.joinHub('test', { 'app': 'node', 'userId': 'none'}, true);
            console.log(`Participants: ${JSON.stringify(relayHubProxy.participants)}`);
            relayHubProxy.onReceiveData((receivedData): Promise<void> =>  {
                if (receivedData.type === 'test') {
                    const msg = decoder.write(<Buffer>receivedData.data);
                    console.log(`received message:${msg}`);
                }
                return Promise.resolve();
            });
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