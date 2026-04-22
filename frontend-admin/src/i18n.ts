import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';

import enTranslation from './locales/en.json';
import esARTranslation from './locales/es-AR.json';

i18n
  .use(initReactI18next)
  .init({
    resources: {
      en: enTranslation,
      'es-AR': esARTranslation
    },
    lng: 'es-AR', // Set Spanish (Argentina) as the default language
    fallbackLng: 'en',
    interpolation: {
      escapeValue: false // react already safes from xss
    }
  });

export default i18n;
