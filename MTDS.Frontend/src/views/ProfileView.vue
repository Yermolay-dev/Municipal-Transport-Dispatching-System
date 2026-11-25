<script setup lang="ts">
import { ref, computed, onMounted, nextTick } from 'vue'
import { useRouter } from 'vue-router'
import apiClient from '../api'

const router = useRouter()

// --- СНЭКБАР УВЕДОМЛЕНИЙ ---
const snackbar = ref({
  show: false,
  text: '',
  color: '#00E676',
})

const showNotice = (text: string, isError: boolean = false) => {
  snackbar.value = {
    show: true,
    text,
    color: isError ? '#FF1744' : '#00E676',
  }
}

// --- СОСТОЯНИЕ ПРОФИЛЯ ---
const isLoadingProfile = ref<boolean>(true)
const isSavingProfile = ref<boolean>(false)
const profileFormRef = ref<any>(null)

const profileData = ref({
  id: '',
  role: localStorage.getItem('role') || 'Dispatcher',
  email: '',
  createdAt: '',
  lastEntranceAt: '',
})

const editableProfile = ref({
  name: '',
  surName: '',
  phoneNumber: '',
})

// Инициалы для круглого v-avatar
const initials = computed(() => {
  const first = editableProfile.value.name?.trim()?.charAt(0) || ''
  const last = editableProfile.value.surName?.trim()?.charAt(0) || ''
  const combo = `${last}${first}`.toUpperCase()
  return combo || 'ОП'
})

const isAdmin = computed(() => profileData.value.role === 'Admin')

const roleDisplayText = computed(() =>
  isAdmin.value ? 'Администратор системы' : 'Оперативный диспетчер'
)

// Форматирование UTC-дат в ДД.ММ.ГГГГ ЧЧ:ММ
const formatDateTime = (dateStr?: string | null): string => {
  if (!dateStr || dateStr.startsWith('0001-01-01')) return 'Никогда'
  const date = new Date(dateStr)
  if (isNaN(date.getTime())) return '—'

  const pad = (n: number) => String(n).padStart(2, '0')
  const day = pad(date.getDate())
  const month = pad(date.getMonth() + 1)
  const year = date.getFullYear()
  const hours = pad(date.getHours())
  const minutes = pad(date.getMinutes())

  return `${day}.${month}.${year} ${hours}:${minutes}`
}

// Автоформатирование телефона при вводе (+7 (XXX) XXX-XX-XX)
const formatPhoneString = (val: string): string => {
  if (!val) return ''
  let digits = val.replace(/\D/g, '')
  if (digits.startsWith('7') || digits.startsWith('8')) {
    digits = digits.slice(1)
  }
  digits = digits.slice(0, 10)
  if (!digits.length) return '+7'

  let formatted = '+7'
  if (digits.length > 0) formatted += ' (' + digits.slice(0, 3)
  if (digits.length >= 3) formatted += ') ' + digits.slice(3, 6)
  if (digits.length >= 6) formatted += '-' + digits.slice(6, 8)
  if (digits.length >= 8) formatted += '-' + digits.slice(8, 10)
  return formatted
}

const onPhoneInput = (e: Event) => {
  const input = e.target as HTMLInputElement
  editableProfile.value.phoneNumber = formatPhoneString(input.value)
}

// --- ПРАВИЛА ВАЛИДАЦИИ ЛИЧНЫХ ДАННЫХ ---
const nameRules = [
  (v: string) => !!v?.trim() || 'Имя обязательно к заполнению',
  (v: string) => (v?.trim().length >= 2 && v?.trim().length <= 50) || 'От 2 до 50 символов',
  (v: string) => /^[а-яА-ЯёЁa-zA-Z\s-]+$/.test(v?.trim()) || 'Допустимы только буквы и дефис',
]

const surNameRules = [
  (v: string) => !!v?.trim() || 'Фамилия обязательна к заполнению',
  (v: string) => (v?.trim().length >= 2 && v?.trim().length <= 50) || 'От 2 до 50 символов',
  (v: string) => /^[а-яА-ЯёЁa-zA-Z\s-]+$/.test(v?.trim()) || 'Допустимы только буквы и дефис',
]

const phoneRegex = /^(\+7\d{10}|\+7 \(\d{3}\) \d{3}-\d{2}-\d{2})$/
const phoneRules = [
  (v: string) => !!v?.trim() || 'Телефон обязателен к заполнению',
  (v: string) => phoneRegex.test(v?.trim() || '') || 'Формат: +7 (XXX) XXX-XX-XX или +7XXXXXXXXXX',
]

const isProfileSubmitAllowed = computed(() => {
  const f = editableProfile.value
  const validName = f.name.trim().length >= 2 && f.name.trim().length <= 50 && /^[а-яА-ЯёЁa-zA-Z\s-]+$/.test(f.name.trim())
  const validSurName = f.surName.trim().length >= 2 && f.surName.trim().length <= 50 && /^[а-яА-ЯёЁa-zA-Z\s-]+$/.test(f.surName.trim())
  const validPhone = phoneRegex.test(f.phoneNumber.trim())

  return Boolean(validName && validSurName && validPhone && !isSavingProfile.value && !isLoadingProfile.value)
})

// Загрузка профиля (GET /profile)
const fetchProfile = async () => {
  try {
    isLoadingProfile.value = true
    const response = await apiClient.get('/profile')
    const raw = response.data?.profile || response.data?.data || response.data || {}

    profileData.value = {
      id: String(raw.id ?? raw.Id ?? ''),
      role: String(raw.role || raw.Role || localStorage.getItem('role') || 'Dispatcher'),
      email: String(raw.email || raw.Email || '—'),
      createdAt: raw.createdAt || raw.CreatedAt || '',
      lastEntranceAt: raw.lastEntranceAt || raw.LastEntranceAt || '',
    }

    const rawPhone = String(raw.phoneNumber || raw.PhoneNumber || '')
    editableProfile.value = {
      name: String(raw.name || raw.Name || ''),
      surName: String(raw.surName || raw.SurName || ''),
      phoneNumber: rawPhone ? formatPhoneString(rawPhone) : '',
    }
  } catch (err: any) {
    console.error('[ОРБИТА-ПРОФИЛЬ] Ошибка загрузки:', err)
    showNotice(err.response?.data?.message || err.message || 'Не удалось загрузить данные профиля', true)
  } finally {
    isLoadingProfile.value = false
  }
}

// Сохранение личных данных (PUT /profile)
const handleSaveProfile = async () => {
  if (!isProfileSubmitAllowed.value) return

  try {
    isSavingProfile.value = true

    const payload = {
      name: editableProfile.value.name.trim(),
      surName: editableProfile.value.surName.trim(),
      phoneNumber: editableProfile.value.phoneNumber.trim(),
    }

    await apiClient.put('/profile', payload)
    window.dispatchEvent(new CustomEvent('profile-updated'))
    showNotice('Личные данные профиля успешно обновлены')
    await fetchProfile()
  } catch (err: any) {
    console.error('[ОРБИТА-ПРОФИЛЬ] Ошибка сохранения:', err)
    showNotice(err.response?.data?.message || err.response?.data?.title || 'Ошибка сохранения личных данных', true)
  } finally {
    isSavingProfile.value = false
  }
}

// ==========================================
// КАРТОЧКА 2: БЕЗОПАСНОСТЬ И СМЕНА ПАРОЛЯ
// ==========================================
const passwordFormRef = ref<any>(null)
const isChangingPassword = ref<boolean>(false)

const showCurrentPassword = ref<boolean>(false)
const showNewPassword = ref<boolean>(false)
const showConfirmPassword = ref<boolean>(false)

const passwordForm = ref({
  currentPassword: '',
  newPassword: '',
  confirmPassword: '',
})

const currentPasswordRules = [(v: string) => !!v || 'Введите текущий пароль']
const newPasswordRules = [
  (v: string) => !!v || 'Введите новый пароль',
  (v: string) => (v && v.length >= 6) || 'Минимальная длина пароля — 6 символов',
]
const confirmPasswordRules = [
  (v: string) => !!v || 'Подтвердите новый пароль',
  (v: string) => v === passwordForm.value.newPassword || 'Введенные пароли не совпадают',
]

const isPasswordSubmitAllowed = computed(() => {
  const p = passwordForm.value
  return Boolean(
    p.currentPassword.length > 0 &&
    p.newPassword.length >= 6 &&
    p.confirmPassword === p.newPassword &&
    !isChangingPassword.value
  )
})

const handleUpdatePassword = async () => {
  if (!isPasswordSubmitAllowed.value) return

  try {
    isChangingPassword.value = true

    await apiClient.patch('/profile/password', {
      currentPassword: passwordForm.value.currentPassword,
      newPassword: passwordForm.value.newPassword,
    })

    showNotice('Пароль учетной записи успешно обновлен')

    // Безопасная очистка полей без срабатывания красной валидации
    passwordForm.value = {
      currentPassword: '',
      newPassword: '',
      confirmPassword: '',
    }
    showCurrentPassword.value = false
    showNewPassword.value = false
    showConfirmPassword.value = false

    await nextTick()
    passwordFormRef.value?.resetValidation()
  } catch (err: any) {
    console.error('[ОРБИТА-ПРОФИЛЬ] Ошибка смены пароля:', err)
    showNotice(err.response?.data?.message || err.response?.data?.title || 'Не удалось обновить пароль', true)
  } finally {
    isChangingPassword.value = false
  }
}

onMounted(() => {
  fetchProfile()
})
</script>

<template>
  <div class="profile-view pa-6">
    <!-- Верхняя панель страницы -->
    <div class="d-flex align-center justify-space-between mb-6 flex-wrap ga-3">
      <div>
        <div class="d-flex align-center">
          <v-icon icon="mdi-account-badge-outline" color="#00E676" size="28" class="mr-2" />
          <h1 class="text-h6 font-weight-black tracking-wider brand-title mb-0">
            ПРОФИЛЬ СОТРУДНИКА
          </h1>
        </div>
        <p class="text-caption text-disabled font-mono mt-1 mb-0">
          ЛИЧНАЯ КАРТОЧКА ОПЕРАТОРА И ПАРАМЕТРЫ БЕЗОПАСНОСТИ ДОСТУПА СДМТ
        </p>
      </div>

      <!-- Кнопка возврата к карте -->
      <v-btn
        variant="outlined"
        color="#00E676"
        rounded="0"
        size="small"
        prepend-icon="mdi-arrow-left"
        class="font-mono font-weight-bold px-4 border-subtle"
        @click="router.push('/')"
      >
        ВЕРНУТЬСЯ К КАРТЕ
      </v-btn>
    </div>

    <!-- Баннер оператора (Аватар с инициалами + Роль) -->
    <v-card class="profile-card pa-5 mb-6" rounded="0">
      <div v-if="isLoadingProfile" class="d-flex align-center py-2">
        <v-progress-circular indeterminate color="#00E676" size="28" width="2" class="mr-4" />
        <span class="font-mono text-caption text-grey-lighten-1">ЗАГРУЗКА ДАННЫХ УЧЕТНОЙ ЗАПИСИ...</span>
      </div>

      <div v-else class="d-flex align-center justify-space-between flex-wrap ga-4">
        <div class="d-flex align-center">
          <v-avatar
            size="64"
            color="#0E1116"
            class="mr-4 avatar-ring"
          >
            <span class="text-h6 font-weight-black font-mono text-green-accent-3">
              {{ initials }}
            </span>
          </v-avatar>

          <div>
            <div class="d-flex align-center flex-wrap ga-2 mb-1">
              <span class="text-h6 font-weight-bold font-mono text-white">
                {{ editableProfile.surName }} {{ editableProfile.name }}
              </span>
              <v-chip
                size="x-small"
                label
                class="font-mono font-weight-bold role-chip"
                :style="{
                  color: isAdmin ? '#FF1744' : '#29B6F6',
                  borderColor: isAdmin ? '#FF1744' : '#29B6F6',
                  backgroundColor: isAdmin ? 'rgba(255, 23, 68, 0.08)' : 'rgba(41, 182, 246, 0.08)',
                }"
                variant="outlined"
              >
                ● {{ isAdmin ? 'АДМИНИСТРАТОР' : 'ДИСПЕТЧЕР' }}
              </v-chip>
            </div>
            <div class="text-caption font-mono text-grey-lighten-1">
              {{ profileData.email }} • {{ roleDisplayText }}
            </div>
          </div>
        </div>

        <div class="text-right d-none d-md-block font-mono">
          <div class="text-caption text-disabled">ИДЕНТИФИКАТОР СИСТЕМЫ</div>
          <div class="text-caption text-grey-lighten-2">{{ profileData.id || '—' }}</div>
        </div>
      </div>
    </v-card>

    <!-- Две основные карточки -->
    <v-row>
      <!-- КАРТОЧКА 1: ЛИЧНЫЕ ДАННЫЕ -->
      <v-col cols="12" md="7">
        <v-card class="profile-card pa-5 fill-height d-flex flex-column" rounded="0">
          <div class="d-flex align-center mb-3">
            <v-icon icon="mdi-card-account-details-outline" color="#29B6F6" size="20" class="mr-2" />
            <span class="text-subtitle-2 font-mono font-weight-bold text-white tracking-wider">
              ЛИЧНЫЕ ДАННЫЕ И СЛУЖЕБНАЯ ИНФОРМАЦИЯ
            </span>
          </div>
          <v-divider class="border-opacity-10 mb-4" />

          <v-form ref="profileFormRef" autocomplete="off" class="flex-grow-1 d-flex flex-column" @submit.prevent="handleSaveProfile">
            <!-- Блок только для чтения -->
            <v-row class="mb-2">
              <v-col cols="12" sm="6" class="py-1">
                <div class="field-label-fixed text-caption text-disabled font-mono mb-1">
                  СЛУЖЕБНЫЙ EMAIL (ЛОГИН)
                </div>
                <v-text-field
                  :model-value="profileData.email"
                  variant="outlined"
                  density="compact"
                  readonly
                  tabindex="-1"
                  prepend-inner-icon="mdi-email-lock-outline"
                  class="font-mono input-readonly input-no-cursor"
                  hide-details
                />
              </v-col>

              <v-col cols="12" sm="6" class="py-1">
                <div class="field-label-fixed text-caption text-disabled font-mono mb-1">
                  ДОЛЖНОСТЬ / РОЛЬ В СИСТЕМЕ
                </div>
                <v-text-field
                  :model-value="roleDisplayText"
                  variant="outlined"
                  density="compact"
                  readonly
                  tabindex="-1"
                  prepend-inner-icon="mdi-shield-account-outline"
                  class="font-mono input-readonly input-no-cursor"
                  hide-details
                />
              </v-col>

              <v-col cols="12" sm="6" class="py-1 mt-2">
                <div class="field-label-fixed text-caption text-disabled font-mono mb-1">
                  ДАТА РЕГИСТРАЦИИ
                </div>
                <v-text-field
                  :model-value="formatDateTime(profileData.createdAt)"
                  variant="outlined"
                  density="compact"
                  readonly
                  tabindex="-1"
                  prepend-inner-icon="mdi-calendar-check-outline"
                  class="font-mono input-readonly input-no-cursor"
                  hide-details
                />
              </v-col>

              <v-col cols="12" sm="6" class="py-1 mt-2">
                <div class="field-label-fixed text-caption text-disabled font-mono mb-1">
                  ПОСЛЕДНИЙ ВХОД В КОНТУР
                </div>
                <v-text-field
                  :model-value="formatDateTime(profileData.lastEntranceAt)"
                  variant="outlined"
                  density="compact"
                  readonly
                  tabindex="-1"
                  prepend-inner-icon="mdi-clock-outline"
                  class="font-mono input-readonly input-no-cursor"
                  hide-details
                />
              </v-col>
            </v-row>

            <v-divider class="border-opacity-10 my-4" />

            <!-- Редактируемые поля -->
            <v-row class="mb-2 align-start">
              <v-col cols="12" sm="6" class="py-1">
                <div class="field-label-fixed text-caption text-disabled font-mono mb-1">ИМЯ *</div>
                <v-text-field
                  v-model="editableProfile.name"
                  placeholder="Иван"
                  variant="outlined"
                  density="compact"
                  color="#00E676"
                  class="font-mono input-tactical"
                  autocomplete="off"
                  hide-details="auto"
                  :rules="nameRules"
                />
              </v-col>

              <v-col cols="12" sm="6" class="py-1">
                <div class="field-label-fixed text-caption text-disabled font-mono mb-1">ФАМИЛИЯ *</div>
                <v-text-field
                  v-model="editableProfile.surName"
                  placeholder="Иванов"
                  variant="outlined"
                  density="compact"
                  color="#00E676"
                  class="font-mono input-tactical"
                  autocomplete="off"
                  hide-details="auto"
                  :rules="surNameRules"
                />
              </v-col>
            </v-row>

            <div class="field-label-fixed text-caption text-disabled font-mono mb-1 mt-1">
              КОНТАКТНЫЙ ТЕЛЕФОН *
            </div>
            <v-text-field
              :model-value="editableProfile.phoneNumber"
              placeholder="+7 (999) 000-00-00"
              variant="outlined"
              density="compact"
              color="#00E676"
              prepend-inner-icon="mdi-phone-outline"
              class="font-mono mb-4 input-tactical"
              autocomplete="off"
              hide-details="auto"
              :rules="phoneRules"
              @input="onPhoneInput"
            />

            <v-spacer />

            <div class="d-flex justify-end mt-2">
              <v-btn
                type="submit"
                color="#00E676"
                variant="flat"
                rounded="0"
                height="40"
                class="font-mono font-weight-bold text-black px-6"
                :loading="isSavingProfile"
                :disabled="!isProfileSubmitAllowed"
              >
                СОХРАНИТЬ ИЗМЕНЕНИЯ
              </v-btn>
            </div>
          </v-form>
        </v-card>
      </v-col>

      <!-- КАРТОЧКА 2: БЕЗОПАСНОСТЬ И СМЕНА ПАРОЛЯ -->
      <v-col cols="12" md="5">
        <v-card class="profile-card pa-5 fill-height d-flex flex-column" rounded="0">
          <div class="d-flex align-center mb-3">
            <v-icon icon="mdi-shield-lock-outline" color="#FFD600" size="20" class="mr-2" />
            <span class="text-subtitle-2 font-mono font-weight-bold text-white tracking-wider">
              БЕЗОПАСНОСТЬ И СМЕНА ПАРОЛЯ
            </span>
          </div>
          <v-divider class="border-opacity-10 mb-4" />

          <v-form ref="passwordFormRef" autocomplete="off" class="flex-grow-1 d-flex flex-column" @submit.prevent="handleUpdatePassword">
            <div class="field-label-fixed text-caption text-disabled font-mono mb-1">
              ТЕКУЩИЙ ПАРОЛЬ *
            </div>
            <v-text-field
              v-model="passwordForm.currentPassword"
              :type="showCurrentPassword ? 'text' : 'password'"
              placeholder="••••••••••••"
              :append-inner-icon="showCurrentPassword ? 'mdi-eye-off-outline' : 'mdi-eye-outline'"
              variant="outlined"
              density="compact"
              color="#FFD600"
              class="font-mono mb-3 input-tactical"
              autocomplete="current-password"
              hide-details="auto"
              :rules="currentPasswordRules"
              @click:append-inner="showCurrentPassword = !showCurrentPassword"
            />

            <div class="field-label-fixed text-caption text-disabled font-mono mb-1">
              НОВЫЙ ПАРОЛЬ (МИНИМУМ 6 СИМВОЛОВ) *
            </div>
            <v-text-field
              v-model="passwordForm.newPassword"
              :type="showNewPassword ? 'text' : 'password'"
              placeholder="••••••••••••"
              :append-inner-icon="showNewPassword ? 'mdi-eye-off-outline' : 'mdi-eye-outline'"
              variant="outlined"
              density="compact"
              color="#00E676"
              class="font-mono mb-3 input-tactical"
              autocomplete="new-password"
              hide-details="auto"
              :rules="newPasswordRules"
              @click:append-inner="showNewPassword = !showNewPassword"
            />

            <div class="field-label-fixed text-caption text-disabled font-mono mb-1">
              ПОДТВЕРДИТЕ НОВЫЙ ПАРОЛЬ *
            </div>
            <v-text-field
              v-model="passwordForm.confirmPassword"
              :type="showConfirmPassword ? 'text' : 'password'"
              placeholder="••••••••••••"
              :append-inner-icon="showConfirmPassword ? 'mdi-eye-off-outline' : 'mdi-eye-outline'"
              variant="outlined"
              density="compact"
              color="#00E676"
              class="font-mono mb-4 input-tactical"
              autocomplete="new-password"
              hide-details="auto"
              :rules="confirmPasswordRules"
              @click:append-inner="showConfirmPassword = !showConfirmPassword"
            />

            <div class="security-note-box pa-3 mb-4 font-mono text-caption text-disabled">
              <v-icon icon="mdi-information-outline" size="15" color="#FFD600" class="mr-1" />
              После смены пароля используйте новый ключ доступа при следующем входе в систему СДМТ.
            </div>

            <v-spacer />

            <div class="d-flex justify-end mt-2">
              <v-btn
                type="submit"
                color="#FFD600"
                variant="flat"
                rounded="0"
                height="40"
                block
                class="font-mono font-weight-bold text-black"
                :loading="isChangingPassword"
                :disabled="!isPasswordSubmitAllowed"
              >
                ОБНОВИТЬ ПАРОЛЬ
              </v-btn>
            </div>
          </v-form>
        </v-card>
      </v-col>
    </v-row>

    <!-- Снэкбар уведомлений -->
    <v-snackbar
      v-model="snackbar.show"
      :color="snackbar.color"
      timeout="4000"
      location="top right"
      rounded="0"
      class="font-mono"
    >
      <div class="d-flex align-center">
        <v-icon
          :icon="snackbar.color === '#FF1744' ? 'mdi-alert-circle-outline' : 'mdi-check-circle-outline'"
          color="white"
          class="mr-2"
        />
        <span class="text-caption font-weight-bold text-white">{{ snackbar.text }}</span>
      </div>
    </v-snackbar>
  </div>
</template>

<style scoped>
.profile-view {
  height: 100%;
  overflow-y: auto;
  background-color: #0A0C10;
}

.brand-title {
  letter-spacing: 0.12em;
  color: #F0F6FC;
}

.border-subtle {
  border-color: #30363D !important;
}

.profile-card {
  background-color: #161B22 !important;
  border: 1px solid #30363D !important;
}

.avatar-ring {
  border: 2px solid #00E676;
  box-shadow: 0 0 12px rgba(0, 230, 118, 0.2);
}

.role-chip {
  letter-spacing: 0.06em;
  font-size: 10px !important;
}

.field-label-fixed {
  min-height: 20px;
  line-height: 20px;
}

.input-tactical {
  background-color: #0E1116;
}

.input-readonly {
  background-color: rgba(14, 17, 22, 0.55);
  opacity: 0.85;
}

.input-no-cursor :deep(input) {
  cursor: default !important;
  pointer-events: none !important;
  color: #8B949E !important;
}

.security-note-box {
  background-color: #0E1116;
  border: 1px solid #21262D;
  line-height: 1.5;
}

.font-mono {
  font-family: 'JetBrains Mono', Consolas, monospace;
}
</style>