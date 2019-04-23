To publish a new version of this package, follow the steps:

0. cd ClientApp/vscode-web
1. yarn update
5. npm version patch
6. npm publish
7. update the version in ClientApp/package.json for the @environments/vscode-web dependency

Having trouble with authentication? See this link on how to set up your machine for the private npm feed: https://docs.microsoft.com/en-us/vsts/package/npm/publish
