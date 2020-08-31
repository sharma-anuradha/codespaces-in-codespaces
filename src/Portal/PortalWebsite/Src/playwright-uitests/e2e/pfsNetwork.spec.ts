/// <reference types="jest-playwright-preset" />
var getEnvironment = require('../e2e/utils/getCodespaceEnvironment');
var getContext = require('../e2e/utils/contextUtil');
import { Page } from 'playwright';
var { ENVIRONMENT } = process.env;

describe('Network UI Tests for Port Forwarding Service', () => {
  let page: Page;
  let codespaceUrl: any;
  let pageTabOne: Page;
  let deleteCodespaceUrl: any;

  beforeAll(async () => {
    await getContext.contextUtil(context);
    page = await context.newPage();
    pageTabOne = await context.newPage();
  }
  );

  it('Should be logged in', async () => {
    await page.goto('https://github.com/codespaces');
    const element = await page.waitForSelector('[aria-label="Codespaces you created"]');
    expect(element).toBeTruthy();
  });

  it('Should create a codespace environment', async () => {
    await page.click('text=New Codespace');
    await page.waitForSelector('[aria-haspopup="menu"]');
    await page.click('summary.btn.btn-sm.d-block.css-truncate');
    await page.fill('[placeholder="Search for a repository"]', 'vso-dev1/PlaywrightTestingApps');
    await page.keyboard.press('ArrowDown');
    await page.click('*css=[role="menuitemradio"] >> text="vso-dev1/PlaywrightTestingApps"');
    const environment = await getEnvironment.getCodespaceEnvironment(ENVIRONMENT);
    await page.click('summary.btn.btn-sm.d-block.css-truncate >> text=' + "'" + 'production' + "'");
    await page.click('*css=[role="menuitemradio"] >> text=' + "'" + environment + "'");
    await page.waitForEvent('requestfinished', {predicate: request => request.url().includes('https://github.com/codespaces/new?repo=')});
    await page.click('[class ="btn btn-sm btn-primary js-new-codespace-submit-button js-toggle-hidden"]');
  });

  it('Should load a codespace environment', async () => {
    await page.waitForSelector('text=Preparing your codespace');
    codespaceUrl = await page.url();
    //Assert that some elements are loaded
    await page.waitForSelector('[aria-label*="Run"]', { timeout: 60000 });
    await page.waitForSelector('[aria-label="Remote Explorer"]');
    await page.click('[aria-label="Remote Explorer"]');
  });

  it('Should launch Testing Apps', async () => {
    deleteCodespaceUrl = await page.url();
    await page.click('[aria-label*="Run "]');
    await page.click('[aria-label="Debug Launch Configurations"]');
    await page.selectOption('[aria-label="Debug Launch Configurations"]', 'Request Body App/PF Echo (Headers)/Web Sockets Echo App');
    await page.keyboard.press('F5');
    await page.click('[title="Start Debugging"]');
    await page.waitForSelector('[aria-label="Debug: Request Body App/PF Echo (Headers)/Web Sockets Echo App (PlaywrightTestingApps)"]');
    await page.waitForSelector('[aria-label="Debug Call Stack"]');
    await page.click('[aria-label="Remote Explorer"]');
    await page.waitForSelector('[aria-label="person vso-dev1"]');
  });

  it('Checks that request bodies are received', async () => {
    const [newPage] = await Promise.all([
      context.waitForEvent('page'),
      page.click('[aria-label="Port: 7000 (port 7000)"]')
    ])
    await newPage.waitForNavigation({ url: url => url.hostname.includes('codespaces.githubusercontent.com') });
    var element = await newPage.waitForSelector('text="Sign in"');
    expect(element).toBeTruthy();
    await newPage.fill('[name="username"]', 'VSCodespaces');
    await newPage.fill('[name="password"]', 'Shipit');
    await newPage.click('[type="submit"]');
    var element = await newPage.waitForSelector('text="POST request: username is VSCodespaces and password is Shipit"');
    expect(element).toBeTruthy();
  });

  it('Checks that user headers added in echo service are propagated', async () => {
    const [newPage] = await Promise.all([
      context.waitForEvent('page'),
      page.click('[aria-label="Port: 5000 (port 5000)"]')
    ])
    await newPage.waitForNavigation({ url: url => url.hostname.includes('codespaces.githubusercontent.com') });
    //Verify that headers added in PF echo app show up in response headers
    var element = await newPage.waitForSelector('[item="PFS"]');
    expect(element).toBeTruthy();
    codespaceUrl = newPage.url();
    element = await newPage.waitForSelector('[item="Cookie"]');
    expect(element).toBeTruthy();
  });

  it('Sends multiple requests to the same endpoint', async () => {
    const arrStr = codespaceUrl.split(/[/.]/);
    const requestsUrl = "https://" + arrStr[2] + ".apps.dev.codespaces.githubusercontent.com/index";
    console.log(requestsUrl)
    await pageTabOne.goto(requestsUrl);
    await pageTabOne.waitForSelector('[item="Calling"]');
    await pageTabOne.reload();
    await pageTabOne.goto(requestsUrl);
    await pageTabOne.waitForSelector('[item="Calling"]');
  });

  //no longer propagate use_vso_pfs and __Host-vso-pf cookies to the user
  it.skip('Checks that user service does not receive auth cookies', async () => {
    const cookies = JSON.stringify(await context.cookies());
    expect(cookies).not.toMatch(/__Host-vso-pf/);
    expect(cookies).not.toMatch(/use_vso_pfs/);
  });

  it('Web Sockets App test', async () => {
    const [newPage] = await Promise.all([
      context.waitForEvent('page'),
      page.click('[aria-label="Port: 3000 (port 3000)"]')
    ])
    await newPage.waitForNavigation({ url: url => url.hostname.includes('codespaces.githubusercontent.com') });
    var element = await newPage.waitForSelector('text="Playing with websockets using socketio"');
    expect(element).toBeTruthy();
    element = await newPage.waitForSelector('[aria-label="servermessages"]');
    expect(element).toBeTruthy();
    //Web sockets connection established
    element = await newPage.waitForSelector('[data-testid="websockets"]');
    expect(element).toBeTruthy();
  });

  it('Should delete the created codespace', async () => {
    await page.goto('https://github.com/codespaces');
    var arrStr = deleteCodespaceUrl.split(/[/.]/);
    await page.waitForSelector('[aria-label=' + "'" + "Show more actions for codespace " + arrStr[2] + "'" + ']');
    await page.click('[aria-label=' + "'" + "Show more actions for codespace " + arrStr[2] + "'" + ']');
    await page.waitForSelector('[aria-label=' + "'" + "Delete codespace " + arrStr[2] + "'" + ']');
    await page.click('[aria-label=' + "'" + "Delete codespace " + arrStr[2] + "'" + ']');
    //Assert that codespace no longer exists 
    await page.waitForSelector('[aria-label=' + "'" + "Delete codespace " + arrStr[2] + "'" + ']', { state: "hidden" });
  });
});
