module.exports = {
  preset: 'jest-playwright-preset',
  testRunner: 'jest-circus/runner',
  testEnvironment: './config/ScreenshotEnv.js',
  transform: {
    '^.+\\.ts$': 'ts-jest',
  },
  globalSetup: './config/authSetup.js',
  setupFilesAfterEnv: ['./config/jest.setup.js']
};