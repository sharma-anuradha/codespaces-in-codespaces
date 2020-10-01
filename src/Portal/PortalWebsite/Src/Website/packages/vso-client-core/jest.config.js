const { pathsToModuleNameMapper } = require('ts-jest/utils');
const { defaults: tsjPreset } = require('ts-jest/presets');
const { compilerOptions } = require('./tsconfig.test.json');

module.exports = {
    ...tsjPreset,
    transform: {
        ...tsjPreset.transform,
    },
    moduleNameMapper: pathsToModuleNameMapper(compilerOptions.paths),
    globals: {
        'ts-jest': {
            tsConfig: compilerOptions,
        },
        'process.env.PORTAL_VERSION': 'test',
    },
    moduleFileExtensions: ['ts', 'tsx', 'js', 'jsx', 'json', 'node'],
    modulePathIgnorePatterns: ['<rootDir>/commonjs/', '<rootDir>/lib/'],
};
