module.exports = {
    roots: ['<rootDir>/'],
    testRegex: '(/__tests__/.*|(\\.|/)(test|spec))\\.tsx?$',
    moduleFileExtensions: ['ts', 'tsx', 'js', 'jsx', 'json', 'node'],
    transformIgnorePatterns: [
        '/node_modules/(?!vso-client-core|vso-workbench|vso-ts-agent).+(js|jsx)$',
    ],
    testPathIgnorePatterns : [
        '<rootDir>/dist/' 
    ],
    modulePathIgnorePatterns: ["<rootDir>/dist/", "<rootDir>/public/"],
    transform: {
        '^.+\\.tsx?$': 'ts-jest',
        '^.+\\.css$': '<rootDir>/jest/cssTransform.js',
        '^.+\\.(js|jsx)$': '<rootDir>/node_modules/babel-jest'
    },
    setupFilesAfterEnv: [
        './jest/jest.setup.js'
    ],
}