# Playwright UI Test Suite

## Features
* **Isolated context per test**: Uses `context` global which is an isolated [browser context](https://playwright.dev/#version=v1.2.1&path=docs%2Fcore-concepts.md&q=browser-contexts) per test.
* **Screenshots on failure**: Auto-capture screenshots of failed test pages (in the `screenshots` directory).
* **Login only once**: Uses `globalSetup` in Jest config to setup a one-time login which is then re-used in tests.
* **TypeScript** support
* **VS Code debugger** support

## Usage

1. Open another Terminal and change directory to the root of the uitests package
```
cd  vsclk-core\src\portal\portalwebsite\src\website\packages\playwright-uitests

```

2. Install dependencies
```
npm install
```

> **Note**: Running tests requires test account credentials set as `USER` and `PASSWORD` environment variables. Use vso-dev1 Github credentials

Run all tests in a particular browser in headful mode
```
 $env:BROWSER="chromium";  $env:USER='';  $env:PASSWORD='';  $env:ENVIRONMENT='dev';  npm test 
```

Run single test by spec name (see [other options in Jest CLI config](https://jestjs.io/docs/en/cli)).
```
 $env:BROWSER="chromium";  $env:USER='';  $env:PASSWORD='';  $env:ENVIRONMENT='dev';  npm e2e/createLoginCreateDelete.spec.ts 
```

## Future work

* Fix Firefox Support
* Add support for junit reports in Azure CLI
* Test the PFS UI under "Codespace Details"
* Replace environment variables with something more cross-platform
