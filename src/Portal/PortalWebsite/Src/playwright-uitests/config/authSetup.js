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
  const context2 = await browser.newContext({
    ignoreHTTPSErrors: true,
    bypassCSP: true,
  });
  const page = await context.newPage();
  const pageTabTwo = await context2.newPage();
  await page.goto('https://github.com/codespaces');
  await page.waitForSelector('text="Sign in to GitHub"');
  await page.fill('[autocomplete="username"]', USER);
  await page.fill('[autocomplete="current-password"]', PASSWORD);
  await page.click('[type="submit"]');

  try {
    await page.waitForSelector('[placeholder="6-digit code"]');
    //Log into outlook if necessary
    await pageTabTwo.goto("https://outlook.live.com/mail/");
    await pageTabTwo.click('[data-task="signin"]');
    await pageTabTwo.fill('[type="email"]', USER + "@outlook.com");
    console.log(USER);
    await pageTabTwo.click('[value="Next"]');
    await pageTabTwo.fill('[placeholder="Password"]', PASSWORD);
    await pageTabTwo.click('[value="Sign in"]');
    await pageTabTwo.click('[value="Yes"]');
    //refresh to get the latest messages
    await pageTabTwo.waitForSelector('[data-is-focusable="true"]');
    await pageTabTwo.waitForSelector('[aria-label*="Verification code:"]');
    await pageTabTwo.click('[aria-label*="Verification code:"]');
    element = await pageTabTwo.$('[aria-label*="Verification code:"]');
    const emailBody = await element.getAttribute("aria-label");
    console.log(emailBody);
    var verificationCode = emailBody.match(/\d/g);
    verificationCode = verificationCode.join("");
    //fetch Github code
    const codeSize = 6;
    verificationCode = verificationCode.slice(verificationCode.length - codeSize);
    console.log(verificationCode);
    await page.fill('[placeholder="6-digit code"]', verificationCode);
    await page.click('[type="submit"]');
  } catch {
    await page.waitForSelector('[aria-label="Codespaces you created"]');
  }

  // Check login is completed (app dependent).
  await page.waitForSelector('[aria-label="Codespaces you created"]');
  process.env.COOKIES = JSON.stringify(await context.cookies());
  process.env.LOCALSTORAGE = await page.evaluate(() => {
    return JSON.stringify(localStorage);
  });
  await page.close();
  await pageTabTwo.close();
};
