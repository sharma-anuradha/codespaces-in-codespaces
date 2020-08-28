'use strict';

(async () => {
  // start loading our settings
  let items;
  let itemsPromise = new Promise((res, req) => {
    chrome.storage.sync.get({
      vscodeChannel: true,
      isEnabled: true,
      codespaceInfo: {},
    }, (items) => {
      res(items);
    });
  });

  const patchForm = (formEl) => {
    if (formEl.id == 'tokenForm') {
      let featureFlags = { };
      let featureFlagsEl = formEl.elements['featureFlags'];
      if (featureFlagsEl) {
        const value = featureFlagsEl.value;
        if (value && value != '') {
          featureFlags = JSON.parse(value);
        }
      } else {
        featureFlagsEl = document.createElement('input');
        featureFlagsEl.name = 'featureFlags';
        featureFlagsEl.type = 'hidden';
        formEl.appendChild(featureFlagsEl);
      }
      featureFlags = { ...featureFlags, ...items.codespaceInfo.featureFlags };
      featureFlagsEl.value = JSON.stringify(featureFlags);
    } else {
      let partnerInfoEl = formEl.elements['partnerInfo'];
      if (partnerInfoEl) {
        try {
          let data = JSON.parse(partnerInfoEl.value);
          //FIXME: Do a proper deep merge
          let newData = { ...data, ...items.codespaceInfo };
          newData.featureFlags = { ...data.featureFlags, ...items.codespaceInfo.featureFlags };
          newData.vscodeSettings = { ...data.vscodeSettings, ...items.codespaceInfo.vscodeSettings };
          partnerInfoEl.value = JSON.stringify(newData);
        } catch (e) {
          console.error(e);
        }
      }
    }
  }

  const submitHandler = async function (e) {
    console.log('patched submit');
    const formEl = e.target;
    if (!formEl) {
      console.warn('e.target was not set; swallowing form.submit');
      return;
    }
    if (!items) {
      items = await itemsPromise;
    }
    if (items.isEnabled) {
      patchForm(formEl);
    }
    formEl.submit();
  };

  document.addEventListener('_formSubmit', submitHandler);

  // the content script global context is different than the document's so we need to inject
  //  a script to the document itself to swizzle the form.submit() method..
  let injectScript = document.createElement('script');
  injectScript.textContent =
    "HTMLFormElement.prototype.submit = function() {\n" +
      "this.dispatchEvent(new Event('_formSubmit', { bubbles: true }));\n" +
    "}";
  document.documentElement.appendChild(injectScript);
})();
