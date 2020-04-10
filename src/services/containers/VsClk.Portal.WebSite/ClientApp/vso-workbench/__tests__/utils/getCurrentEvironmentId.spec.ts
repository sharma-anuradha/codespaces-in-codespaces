import { KNOWN_VSO_HOSTNAMES } from 'vso-client-core';
import { getCurrentEnvironmentId } from '../../src/utils/getCurrentEnvironmentId';

describe('getCurrentEnvironmentId', () => {
    const { location } = window;

    afterAll(() => {
        window.location = location;
    });

    it('should get `environmentId` (workspace)', () => {
        delete window.location;

        const envId = '76673b75-4ce1-4d97-8eac-04c29c062b3c';
        const url = `https://online.dev.core.vsengsaas.visualstudio.com/workspace/${envId}`;

        window.location = new URL(url) as any;

        const id = getCurrentEnvironmentId();

        expect(envId).toBe(id);
    });

    it('should get `environmentId` (environment)', () => {
        delete window.location;

        const envId = '76673b75-4ce1-4d97-8eac-04c29c062b3c';
        const url = `https://online.dev.core.vsengsaas.visualstudio.com/environment/${envId}`;

        window.location = new URL(url) as any;

        const id = getCurrentEnvironmentId();

        expect(envId).toBe(id);
    });

    it('should ignore query params', () => {
        delete window.location;

        const envId = '76673b75-4ce1-4d97-8eac-04c29c062b3c';
        const url = `https://online.dev.core.vsengsaas.visualstudio.com/workspace/${envId}?autoStart=true&something=1`;

        window.location = new URL(url) as any;

        const id = getCurrentEnvironmentId();

        expect(envId).toBe(id);
    });

    it('should ignore fragments', () => {
        delete window.location;

        const envId = '76673b75-4ce1-4d97-8eac-04c29c062b3c';
        const url = `https://online.dev.core.vsengsaas.visualstudio.com/workspace/${envId}#fragment`;

        window.location = new URL(url) as any;

        const id = getCurrentEnvironmentId();

        expect(envId).toBe(id);
    });

    it('should ignore params and fragments', () => {
        delete window.location;

        const envId = '76673b75-4ce1-4d97-8eac-04c29c062b3c';
        const url = `https://online.dev.core.vsengsaas.visualstudio.com/workspace/${envId}?autoStart=true#fragment`;

        window.location = new URL(url) as any;

        const id = getCurrentEnvironmentId();

        expect(envId).toBe(id);
    });

    it('should throw on the wrong environment id format', () => {
        delete window.location;

        const wrongFormates = [
            '76673b754ce14d978eac04c29c062b3c',
            '76673b754ce1-4d97-8eac-04c29c062b3c',
            '76673b75-4ce1-4d978eac-04c29c062b3c',
            '76673b75-4ce1-4d9-78eac04c29c062b3c',
            '76673b75-4ce14d9-78eac04c29c062b3c',
            '76673b754ce14d9-78eac04c29c062b3c',
            '76673b754ce14d9',
        ];

        for (let envId of wrongFormates) {
            const url = `https://online.dev.core.vsengsaas.visualstudio.com/workspace/${envId}`;

            window.location = new URL(url) as any;

            expect(getCurrentEnvironmentId).toThrow();
        }
    });

    it('should throw on wrong path', () => {
        delete window.location;

        const url = `https://online.dev.core.vsengsaas.visualstudio.com/envs/76673b75-4ce1-4d97-8eac-04c29c062b3c`;

        window.location = new URL(url) as any;

        expect(getCurrentEnvironmentId).toThrow();
    });

    it('should throw on wrong hostnames', () => {
        delete window.location;

        const url = `https://online-test.dev.core.vsengsaas.visualstudio.com/environment/76673b75-4ce1-4d97-8eac-04c29c062b3c`;
        window.location = new URL(url) as any;
        expect(getCurrentEnvironmentId).toThrow();
    });

    it('should throw on wrong url', () => {
        delete window.location;

        const wrongUrls = [
            'https://online-test.dev.core.vsengsaas.visualstudio.com/environment/76673b75-4ce1-4d97-8eac-04c29c062b3c',
            'https://github.com/workspace/76673b75-4ce1-4d97-8eac-04c29c062b3c',
            'https://online-test.dev.core.vsengsaas.visualstudio.com/76673b75-4ce1-4d97-8eac-04c29c062b3c',
            'https://76673b75-4ce1-4d97-8eac-04c29c062b3c.online-test.dev.core.vsengsaas.visualstudio.com/environment',
            'https://76673b75-4ce1-4d97-8eac-04c29c062b3c.environment.online-test.dev.core.vsengsaas.visualstudio.com',
        ];

        for (let urlString of wrongUrls) {
            window.location = new URL(urlString) as any;

            expect(getCurrentEnvironmentId).toThrow();
        }
    });

    it('should not throw on known hostnames', () => {
        delete window.location;

        for (let hostname of KNOWN_VSO_HOSTNAMES) {
            const url = `https://${hostname}/workspace/76673b75-4ce1-4d97-8eac-04c29c062b3c`;
            window.location = new URL(url) as any;
            expect(getCurrentEnvironmentId).not.toThrow();
        }
    });
});
