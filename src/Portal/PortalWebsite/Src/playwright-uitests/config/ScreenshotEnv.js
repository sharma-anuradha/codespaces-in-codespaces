const PlaywrightEnvironment = require('jest-playwright-preset/lib/PlaywrightEnvironment').default;

class ScreenshotAfterFailureEnv extends PlaywrightEnvironment {
  async handleTestEvent(event) {
    if (event.name === 'test_done' && event.test.errors.length > 0) {
      const parentName = event.test.parent.name.replace(/\W/g, '-')
      const specName = event.test.name.replace(/\W/g, '-')
      const contextPages = this.global.context.pages();

      if (contextPages.entries() > 0) {
        for (let [index, page] of contextPages.entries()) {
          await page.screenshot({
            path: `screenshots/${parentName}_${specName}_page${index}.png`,
          })
        }
      }
    }
  }
}

module.exports = ScreenshotAfterFailureEnv