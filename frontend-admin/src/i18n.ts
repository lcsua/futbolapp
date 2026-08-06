import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';

import enTranslation from './locales/en.json';
import esARTranslation from './locales/es-AR.json';

i18n
  .use(initReactI18next)
  .init({
    resources: {
      en: enTranslation,
      'es-AR': esARTranslation,
      es: esARTranslation,
    },
    lng: 'es-AR',
    fallbackLng: 'es-AR',
    supportedLngs: ['es-AR', 'es', 'en'],
    nonExplicitSupportedLngs: true,
    interpolation: {
      escapeValue: false
    }
  });

export default i18n;
