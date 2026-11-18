import 'vuetify/styles'
import '@mdi/font/css/materialdesignicons.css'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import { ru } from 'vuetify/locale' // <-- Импортируем русский язык из Vuetify

export default createVuetify({
  components,
  directives,
  locale: {
    locale: 'ru',       // Основной язык
    fallback: 'en',
    messages: { ru },   // Подключаем словарь
  },
  theme: {
    defaultTheme: 'dark',
    themes: {
      dark: {
        dark: true,
        colors: {
          background: '#0A0C10',
          surface: '#161B22',
          primary: '#00E676',
          error: '#FF1744',
          warning: '#FFD600',
          info: '#29B6F6',
        },
      },
    },
  },
})