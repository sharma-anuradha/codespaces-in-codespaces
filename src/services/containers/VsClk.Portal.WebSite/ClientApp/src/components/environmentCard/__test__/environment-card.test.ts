import {
    buildEnvironmentSettingsUpdateRequest,
    getDropdownOptionForSettingsUpdates,
} from '../environment-card';
import { ILocalCloudEnvironment } from '../../../interfaces/cloudenvironment';
import { ActivePlanInfo } from '../../../reducers/plans-reducer';

describe('environment-card', () => {
    describe('buildEnvironmentSettingsUpdateRequest', () => {
        const environment = <ILocalCloudEnvironment>{
            skuName: 'skuName',
            autoShutdownDelayMinutes: 0,
        };

        it('returns null if no settings are changed', async () => {
            expect(
                buildEnvironmentSettingsUpdateRequest(
                    environment,
                    environment.skuName,
                    environment.autoShutdownDelayMinutes
                )
            ).toBeNull();
        });

        it('returns a request for an update with a single change', async () => {
            const newSkuName = 'newSkuName';
            expect(
                buildEnvironmentSettingsUpdateRequest(
                    environment,
                    newSkuName,
                    environment.autoShutdownDelayMinutes
                )
            ).toEqual({
                skuName: newSkuName,
            });
        });

        it('returns a request for an update with multiple settings', async () => {
            const newSkuName = 'newSkuName';
            const newSuspendDelay = 123;
            expect(
                buildEnvironmentSettingsUpdateRequest(environment, newSkuName, newSuspendDelay)
            ).toEqual({
                skuName: newSkuName,
                autoShutdownDelayMinutes: newSuspendDelay,
            });
        });
    });

    describe('getDropdownOptionForSettingsUpdates', () => {
        const sku1 = {
            name: 'sku1',
            displayName: 'sku1Name',
            os: 'linux',
        };

        const sku2 = {
            name: 'sku2',
            displayName: 'sku2Name',
            os: 'linux',
        };

        const plan = <ActivePlanInfo>{
            availableSkus: [sku1, sku2],
        };

        const environment = <ILocalCloudEnvironment>{
            skuName: sku1.name,
            autoShutdownDelayMinutes: 0,
        };

        it('does not add current settings if present', async () => {
            expect(
                getDropdownOptionForSettingsUpdates(
                    {
                        allowedAutoShutdownDelayMinutes: [0, 1],
                        allowedSkus: [sku1, sku2],
                    },
                    environment,
                    plan
                )
            ).toEqual({
                skuEditOptions: [
                    {
                        key: sku1.name,
                        text: sku1.displayName,
                    },
                    {
                        key: sku2.name,
                        text: sku2.displayName,
                    },
                ],
                autoShutdownDelayEditOptions: [
                    {
                        key: 0,
                        text: 'Never',
                    },
                    {
                        key: 1,
                        text: 'After 1 minutes',
                    },
                ],
            });
        });

        it('adds current settings if not present', async () => {
            expect(
                getDropdownOptionForSettingsUpdates(
                    {
                        allowedAutoShutdownDelayMinutes: [1],
                        allowedSkus: [sku2],
                    },
                    environment,
                    plan
                )
            ).toEqual({
                skuEditOptions: [
                    {
                        key: sku1.name,
                        text: sku1.displayName,
                    },
                    {
                        key: sku2.name,
                        text: sku2.displayName,
                    },
                ],
                autoShutdownDelayEditOptions: [
                    {
                        key: 0,
                        text: 'Never',
                    },
                    {
                        key: 1,
                        text: 'After 1 minutes',
                    },
                ],
            });
        });
    });
});
