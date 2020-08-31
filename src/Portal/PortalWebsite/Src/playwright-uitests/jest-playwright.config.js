const { BROWSER, HEADLESS } = process.env;
const isHeadful = HEADLESS === 0 || HEADLESS === 'false';

module.exports = {
  browsers: BROWSER ? [BROWSER] : ['chromium', 'firefox'],
  launchOptions: {
    headless: !isHeadful
  }
}