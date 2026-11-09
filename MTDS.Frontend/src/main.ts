import { createApp } from 'vue'
import App from './App.vue'
import vuetify from './plugins/vuetify'
import router from './router' // <-- 1. Импорт роутера

const app = createApp(App)

app.use(vuetify)
app.use(router) // <-- 2. Регистрация роутера ОБЯЗАТЕЛЬНО ДО mount()

app.mount('#app')