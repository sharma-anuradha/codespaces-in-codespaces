const { pathsToModuleNameMapper } = require('ts-jest/utils');
const { defaults: tsjPreset } = require('ts-jest/presets');
const { compilerOptions } = require('./tsconfig.test.json');

module.exports = {
    ...tsjPreset,
    transform: {
        ...tsjPreset.transform,
        '^.+\\.css$': '<rootDir>/jest/cssTransform.js',
    },
    moduleNameMapper: {
        ...pathsToModuleNameMapper(compilerOptions.paths),
        'office-ui-fabric-react/lib/(.+)': 'office-ui-fabric-react/lib-commonjs/$1',
        '@uifabric/fluent-theme/lib/(.+)': '@uifabric/fluent-theme/lib-commonjs/$1',
    },
    globals: {
        'ts-jest': {
            tsConfig: compilerOptions,
        },
        __PORTAL_VERSION__: 'test',
    },
    testEnvironment: 'jsdom',
    moduleFileExtensions: ['ts', 'tsx', 'js', 'jsx', 'json', 'node'],
    modulePathIgnorePatterns: ['<rootDir>/commonjs/', '<rootDir>/lib/'],
    setupFilesAfterEnv: ['./jest/jest.setup.js'],
};
