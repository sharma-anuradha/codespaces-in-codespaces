import { ILocalEnvironment } from 'vso-client-core';

import {
    buildEnvironmentSettingsUpdateRequest,
    getDropdownOptionForSettingsUpdates,
} from '../environment-card';
import { ActivePlanInfo } from '../../../reducers/plans-reducer';

const englishStrings = require("../../../loc/resources/WebsiteStringResources.json");

const mockTranslationFunc = (key: string) => {
    if (!key) {
        return undefined;
    }
    return englishStrings[key];
}

describe('environment-card', () => {
    describe('buildEnvironmentSettingsUpdateRequest', () => {
        const environment = <ILocalEnvironment>{
            skuName: 'skuName',
            autoShutdownDelayMinutes: 5,
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

        const environment = <ILocalEnvironment>{
            skuName: sku1.name,
            autoShutdownDelayMinutes: 5,
        };

        it('does not add current settings if present', async () => {
            expect(
                getDropdownOptionForSettingsUpdates(
                    {
                        allowedAutoShutdownDelayMinutes: [5, 30],
                        allowedSkus: [sku1, sku2],
                    },
                    environment,
                    plan,
                    mockTranslationFunc
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
                        key: 5,
                        text: 'After 5 minutes',
                    },
                    {
                        key: 30,
                        text: 'After 30 minutes',
                    },
                ],
            });
        });

        it('adds current settings if not present', async () => {
            expect(
                getDropdownOptionForSettingsUpdates(
                    {
                        allowedAutoShutdownDelayMinutes: [30],
                        allowedSkus: [sku2],
                    },
                    environment,
                    plan,
                    mockTranslationFunc
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
                        key: 5,
                        text: 'After 5 minutes',
                    },
                    {
                        key: 30,
                        text: 'After 30 minutes',
                    },
                ],
            });
        });
    });
});
