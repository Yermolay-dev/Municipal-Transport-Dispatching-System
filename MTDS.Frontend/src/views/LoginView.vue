<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import apiClient from '../api'

const router = useRouter()

// Поля формы
const email = ref<string>('')
const password = ref<string>('')
const showPassword = ref<boolean>(false)
const isLoading = ref<boolean>(false)

// Уведомление об ошибке
const snackbar = ref<boolean>(false)
const errorMessage = ref<string>('')

// Правила валидации Vuetify
const rules = {
  required: (v: string) => !!v || 'Поле обязательно для заполнения',
  email: (v: string) => /.+@.+\..+/.test(v) || 'Введите корректный Email',
}

const handleLogin = async () => {
  if (!email.value || !password.value) {
    errorMessage.value = 'Заполните все поля'
    snackbar.value = true
    return
  }

  try {
    isLoading.value = true

    // Отправляем запрос на авторизацию
    const response = await apiClient.post<{ token: string; role: string }>('/auth/login', {
      email: email.value,
      password: password.value,
    })

    const { token, role } = response.data

    // Сохраняем сессионные данные
    localStorage.setItem('token', token)
    localStorage.setItem('role', role)

    // Переходим на оперативный пульт
    await router.push('/')
  } catch (error: any) {
    if (error.response && error.response.status === 401) {
      errorMessage.value = 'Неверный логин или пароль'
    } else {
      errorMessage.value = 'Ошибка связи с сервером авторизации'
    }
    snackbar.value = true
  } finally {
    isLoading.value = false
  }
}
</script>

<template>
  <div class="login-viewport d-flex align-center justify-center">
    <div class="tactical-grid" />

    <v-card class="login-card pa-6" elevation="0" width="100%" max-width="400">
      <!-- Заголовок системы -->
      <div class="d-flex align-center justify-center mb-6">
        <v-icon icon="mdi-radar" color="#00E676" size="32" class="mr-3" />
        <div>
          <div class="brand-title text-h6 font-weight-black">ОРБИТА</div>
          <div class="text-caption text-disabled tracking-widest font-mono">СДМТ • СЛУЖБА ДИСПЕТЧЕРИЗАЦИИ</div>
        </div>
      </div>

      <v-form @submit.prevent="handleLogin">
        <!-- Поле Email -->
        <div class="text-caption text-disabled mb-1 font-mono">ИДЕНТИФИКАТОР ОПЕРАТОРА (EMAIL)</div>
        <v-text-field
          v-model="email"
          placeholder="dispatcher@orbita.local"
          prepend-inner-icon="mdi-account-outline"
          variant="outlined"
          density="compact"
          color="#00E676"
          :rules="[rules.required, rules.email]"
          class="mb-3 font-mono input-sharp"
          autocomplete="email"
        />

        <!-- Поле Пароля -->
        <div class="text-caption text-disabled mb-1 font-mono">ПАРОЛЬ ДОСТУПА</div>
        <v-text-field
          v-model="password"
          :type="showPassword ? 'text' : 'password'"
          placeholder="••••••••••••"
          prepend-inner-icon="mdi-lock-outline"
          :append-inner-icon="showPassword ? 'mdi-eye-off-outline' : 'mdi-eye-outline'"
          variant="outlined"
          density="compact"
          color="#00E676"
          :rules="[rules.required]"
          class="mb-4 font-mono input-sharp"
          autocomplete="current-password"
          @click:append-inner="showPassword = !showPassword"
        />

        <!-- Кнопка Входа -->
        <v-btn
          type="submit"
          block
          rounded="0"
          height="42"
          color="#00E676"
          :loading="isLoading"
          class="auth-btn font-mono font-weight-bold"
        >
          ВОЙТИ В КОНТУР
        </v-btn>
      </v-form>

      <div class="text-center mt-6">
        <span class="text-caption text-disabled font-mono" style="font-size: 10px !important;">
          ДОСТУП ТОЛЬКО ДЛЯ АВТОРИЗОВАННОГО ПЕРСОНАЛА СДМТ
        </span>
      </div>
    </v-card>

    <!-- Всплывающая ошибка -->
    <v-snackbar
      v-model="snackbar"
      color="#1A1013"
      location="top"
      timeout="4000"
      class="snackbar-error font-mono"
      rounded="0"
    >
      <div class="d-flex align-center">
        <v-icon icon="mdi-alert-circle-outline" color="#FF1744" class="mr-2" />
        <span class="text-caption text-red-lighten-2">{{ errorMessage }}</span>
      </div>
    </v-snackbar>
  </div>
</template>

<style scoped>
.login-viewport {
  position: relative;
  width: 100vw;
  height: 100vh;
  background: radial-gradient(circle at center, #13171F 0%, #0A0C10 100%);
  overflow: hidden;
}

.tactical-grid {
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background-image:
    linear-gradient(rgba(255, 255, 255, 0.02) 1px, transparent 1px),
    linear-gradient(90deg, rgba(255, 255, 255, 0.02) 1px, transparent 1px);
  background-size: 40px 40px;
  pointer-events: none;
}

.login-card {
  position: relative;
  z-index: 2;
  background-color: #161B22 !important;
  border: 1px solid #30363D !important;
  border-radius: 4px;
}

.brand-title {
  letter-spacing: 0.15em;
  color: #F0F6FC;
}

.auth-btn {
  color: #0A0C10 !important;
  letter-spacing: 0.08em;
  transition: opacity 0.2s;
}

.auth-btn:hover {
  opacity: 0.9;
}

.snackbar-error {
  border: 1px solid #FF1744 !important;
}

.font-mono {
  font-family: "JetBrains Mono", Consolas, monospace;
}
</style>