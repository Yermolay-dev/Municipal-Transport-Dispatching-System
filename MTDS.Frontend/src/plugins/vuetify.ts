// Импортируем стили Vuetify и шрифт иконок MDI
import 'vuetify/styles'
import '@mdi/font/css/materialdesignicons.css'

// Импортируем сам Vuetify и базовые функции
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'

export default createVuetify({
  components,
  directives,
  theme: {
    defaultTheme: 'dark',
    themes: {
      dark: {
        dark: true,
        colors: {
          background: '#0A0C10',
          surface: '#161B22',
          primary: '#00E676',   // Неоново-зеленый (норма)
          error: '#FF1744',     // Неоново-красный (ЧП)
          warning: '#FFD600',   // Желтый (отклонение)
          info: '#29B6F6',
        },
      },
    },
  },
})