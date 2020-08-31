const { chromium } = require('playwright');
const { USER, PASSWORD, HEADLESS } = process.env;
const isHeadful = HEADLESS === 0 || HEADLESS === 'false';

module.exports = async() => {
  // This step can use any browser.
  const browser = await chromium.launch({ headless: !isHeadful });
  const context = await browser.newContext({
    ignoreHTTPSErrors: true,
    bypassCSP: true,
  });
  const page = await context.newPage();
  await page.goto('https://github.com/codespaces');
  await page.waitForSelector('text="Sign in to GitHub"');
  await page.fill('[autocomplete="username"]', USER);
  await page.fill('[autocomplete="current-password"]', PASSWORD);
  await page.click('[type="submit"]');

  // Check login is completed (app dependent).
  await page.waitForSelector('[aria-label="Codespaces you created"]');
  process.env.COOKIES = JSON.stringify(await context.cookies());
  process.env.LOCALSTORAGE = await page.evaluate(() => JSON.stringify(localStorage));
  await page.close();
};