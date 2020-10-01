import { EnvironmentStateInfo, EnvironmentType, IEnvironment } from "vso-client-core";

const envInfo: IEnvironment = {
    id: '8f889227-948e-4391-8a91-8e83523de9b8',
    type: EnvironmentType.CloudEnvironment,
    friendlyName: "legomushroom-depot-jwg7",
    created: new Date('2020-09-30T18:38:25.6809872Z'),
    updated: new Date('2020-09-30T18:38:41.1787315Z'),
    state: EnvironmentStateInfo.Available,
    seed: {
        type: 'Git',
        moniker: 'https://github.com/legomushroom/depot',
    },
    connection:{
        sessionId: 'CE9EED39FB769F9A702C62B5D51B4ADBF4E4',
        sessionPath: '',
    },
    location: 'WestUs2',
    planId: '/subscriptions/d833c9b9-c971-47f1-8156-c4236552bdfd/resourceGroups/WestUs2-195/providers/Microsoft.Codespaces/plans/plan-99ec0ba0-193d-4c09-93f0-ac54db61e486',
    autoShutdownDelayMinutes: 30,
    skuName: 'basicLinux',
    skuDisplayName: 'Basic (Linux): 2 cores, 4 GB RAM',
    lastStateUpdateReason: '',
};

export const getEnvironmentInfo = () => {
    return {
        ...envInfo
    };
};
