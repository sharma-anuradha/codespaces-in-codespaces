const { pathsToModuleNameMapper } = require('ts-jest/utils');
const { defaults: tsjPreset } = require('ts-jest/presets');
const { compilerOptions } = require('./tsconfig.test.json');

module.exports = {
    ...tsjPreset,
    transform: {
        ...tsjPreset.transform,
        '^.+\\.css$': '<rootDir>/jest/cssTransform.js',
    },
    moduleNameMapper: pathsToModuleNameMapper(compilerOptions.paths),
    globals: {
        'ts-jest': {
            tsConfig: compilerOptions,
        },
        __PORTAL_VERSION__: 'test',
    },
    moduleFileExtensions: ['ts', 'tsx', 'js', 'jsx', 'json', 'node'],
    modulePathIgnorePatterns: ['<rootDir>/commonjs/', '<rootDir>/lib/'],
    setupFilesAfterEnv: ['./jest/jest.setup.js'],
};
