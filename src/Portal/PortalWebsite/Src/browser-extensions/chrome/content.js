'use strict';

(async () => {
  const getItemsAsync = () => {
    return new Promise((res, req) => {
      chrome.storage.sync.get({
        vscodeChannel: true,
        isEnabled: true,
        isError: true
      }, (items) => {
        res(items);
      });
    });
  };

  const items = await getItemsAsync();

  if (!items.isEnabled) {
    console.log(`Extension is not enabled.`);
    return;
  }

  let intervalsCount = 0;
  let interval;
  
  const errorOut = () => {
    clearInterval(interval);

    chrome.storage.sync.set({ isError: true });

    chrome.runtime.sendMessage({
      action: 'updateIcon',
      isEnabled: items.isEnabled,
      channel: items.vscodeChannel,
      isError: true
    });
  }

  interval = setInterval(() => {
    if (intervalsCount > 10) {
      errorOut();
      return;
    }

    try {
      const iframe = document.querySelector('.js-codespace-iframe-container iframe');
      if (!iframe) {
        console.warn(`No iframe found. [attempt ${intervalsCount}]`);
        intervalsCount++;
        return;
      }
      
      const src = iframe.src;
      
      const url = new URL(src);
      
      url.pathname = '/codespace';

      if (items.vscodeChannel) {
        url.searchParams.set('dogfoodChannel', items.vscodeChannel);
      }

      clearInterval(interval);

      chrome.runtime.sendMessage({
        action: 'updateIcon',
        isEnabled: items.isEnabled,
        channel: items.vscodeChannel,
        isError: false
      });

      chrome.storage.sync.set({ isError: false });

      iframe.src = url.toString();
    } catch (e) {
      console.error(e);
      errorOut();
    }
  }, 25);
})();
