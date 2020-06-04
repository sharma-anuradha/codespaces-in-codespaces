import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';

import LanguageDetector from 'i18next-browser-languagedetector';

const englishStrings = require("./resources/WebsiteStringResources.json");

i18n
  //Browser language detection
  .use(LanguageDetector)
  //Pass i18n instance to react-i18next.
  .use(initReactI18next)
  .init({
    resources: {
      en: {
        translation: englishStrings,
      }
    },
    fallbackLng: 'en',
    debug: false, //Enable it to get more logging
    keySeparator: false, //Using the complete key string
    interpolation: {
      escapeValue: false, //not needed for react as it escapes by default
    }
  });

export default i18n;