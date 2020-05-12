# Visual Studio Codespaces

## Tip & Tricks

When looking at debug logs in chrome, you can exclude the common & browser files from stack traces by marking them as library code.

## Playwright automation

**npx playwright-web**
This will lauch the recorder in the web with localhost:8080 port number by default.

- to process the json that is saved from playwright recorder before checking it into the repo, 
**npm run test:ui-prep-json -- --env=dev --user=<emailaddress> --password=<value> --file="C:\Users\*****\OneDrive - Microsoft\Desktop\temp\vso-sanity.json"** 
 This command will take the json and removes the user id, password & url info in it and place it under your temp directory test-prep folder.
**NOTE:** please make sure to have "" quotes, if there is a space in this path. 

- in order to perform test in the local machine the command is
   **npm run test:ui -- --env=local --user=<emailaddress> --password=<value>** to perform the test JSON files synchronously inside the src/portal/test/actions directory.
   They are executed in debug mode, you will be able to see them lauching the browser.

- if we want to test a particular json file and the command for running in headless mode, then run the following command with the unmodified json file from the playwright recorder.
  **npx playwright-cli --verbose "C:\Users\*****\OneDrive - Microsoft\Desktop\temp\vso-sanity.json"**

- if we want to run it in debug mode then run the following command with the unmodified json file that we have got from the recorder.
 **npx playwright-cli --debug --verbose "C:\Users\*****\OneDrive - Microsoft\Desktop\temp\vso-sanity.json"**

