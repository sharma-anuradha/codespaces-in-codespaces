chrome.runtime.onInstalled.addListener(() => {
  chrome.declarativeContent.onPageChanged.removeRules(undefined, () => {
    chrome.declarativeContent.onPageChanged.addRules([{
      conditions: [new chrome.declarativeContent.PageStateMatcher({ pageUrl: { hostEquals: 'github.com' }, })
    ],
    actions: [new chrome.declarativeContent.ShowPageAction()]
  }]);
});
});

const updateIcon = (msg) => {
  if (!msg.isEnabled) {
    return chrome.browserAction.setIcon({
      path: `/images/disabled@8x.png`
    });
  }

  if (msg.isError) {
    return chrome.browserAction.setIcon({
      path: `/images/error@8x.png`
    });
  }

  if (msg.channel) {
    chrome.browserAction.setIcon({
      path: `/images/${msg.channel}@8x.png`
    });
  }
}

chrome.runtime.onMessage.addListener((msg, sender, sendResponse) => {
  if (msg.action === 'updateIcon') {
    updateIcon(msg);
  }
});

// Restores select box and checkbox state using the preferences
  // stored in chrome.storage.
  function restoreOptions() {
    chrome.storage.sync.get({
        vscodeChannel: true,
        isEnabled: true
    }, function(items) {
        const isEnabled = typeof items.isEnabled === 'boolean'
            ? items.isEnabled
            : true;

        document.body.classList.toggle('is-enabled', isEnabled);

        updateIcon({
          channel: items.vscodeChannel,
          isEnabled,
        });
    });
}

  document.addEventListener('DOMContentLoaded', restoreOptions);