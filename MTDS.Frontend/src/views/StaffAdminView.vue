<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import apiClient from '../api'

interface StaffMember {
  id: string | number
  name?: string
  surName?: string
  fullName: string
  email: string
  phoneNumber?: string
  role: string
  lastEntranceAt?: string | null
  createdAt?: string
}

const search = ref<string>('')
const isLoading = ref<boolean>(false)
const staffList = ref<StaffMember[]>([])

// Уведомления (Snackbar)
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

// --- ОПРЕДЕЛЕНИЕ ТЕКУЩЕГО ВОШЕДШЕГО АДМИНИСТРАТОРА ---
const getCurrentAuthUser = (): { id: string; email: string } => {
  const token = localStorage.getItem('token')
  if (!token) return { id: '', email: '' }
  try {
    const base64Url = token.split('.')[1]
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/')
    const jsonPayload = decodeURIComponent(
      atob(base64)
        .split('')
        .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join('')
    )
    const payload = JSON.parse(jsonPayload)

    const id =
      payload.sub ||
      payload.id ||
      payload.nameid ||
      payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ||
      ''

    const email =
      payload.email ||
      payload.unique_name ||
      payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] ||
      ''

    return {
      id: String(id).toLowerCase().trim(),
      email: String(email).toLowerCase().trim(),
    }
  } catch {
    return { id: '', email: '' }
  }
}

const currentAuthUser = ref(getCurrentAuthUser())

const isSelf = (staff: StaffMember): boolean => {
  if (!staff) return false
  const staffId = String(staff.id || '').toLowerCase().trim()
  const staffEmail = String(staff.email || '').toLowerCase().trim()

  const matchId = currentAuthUser.value.id && staffId && currentAuthUser.value.id === staffId
  const matchEmail = currentAuthUser.value.email && staffEmail && currentAuthUser.value.email === staffEmail

  return Boolean(matchId || matchEmail)
}

// Конфигурация колонок таблицы
const headers = [
  { title: 'ФИО СОТРУДНИКА', key: 'fullName', align: 'start' as const },
  { title: 'EMAIL', key: 'email', align: 'start' as const },
  { title: 'ТЕЛЕФОН', key: 'phoneNumber', align: 'start' as const },
  { title: 'РОЛЬ ДОСТУПА', key: 'role', align: 'center' as const, width: '160px' },
  { title: 'ПОСЛЕДНИЙ ВХОД', key: 'lastEntranceAt', align: 'start' as const },
  { title: 'ДЕЙСТВИЯ', key: 'actions', align: 'center' as const, sortable: false, width: '120px' },
]

const roleSelectOptions = [
  { title: 'Оперативный диспетчер', value: 'Dispatcher' },
  { title: 'Администратор системы', value: 'Admin' },
]

// Преобразование UTC-даты в местное время (ДД.ММ.ГГГГ ЧЧ:ММ)
const formatDateTime = (dateStr?: string | null): string => {
  if (!dateStr || dateStr.startsWith('0001-01-01')) return 'Никогда'
  const date = new Date(dateStr)
  if (isNaN(date.getTime())) return 'Никогда'

  const pad = (n: number) => String(n).padStart(2, '0')
  const day = pad(date.getDate())
  const month = pad(date.getMonth() + 1)
  const year = date.getFullYear()
  const hours = pad(date.getHours())
  const minutes = pad(date.getMinutes())

  return `${day}.${month}.${year} ${hours}:${minutes}`
}

// Загрузка реестра сотрудников (GET /auth/staff)
const fetchStaff = async () => {
  try {
    isLoading.value = true
    const response = await apiClient.get('/auth/staff')

    let rawList: any[] = []
    if (Array.isArray(response.data)) {
      rawList = response.data
    } else if (response.data && Array.isArray(response.data.staff)) {
      rawList = response.data.staff
    } else if (response.data && Array.isArray(response.data.items)) {
      rawList = response.data.items
    } else if (response.data && Array.isArray(response.data.data)) {
      rawList = response.data.data
    }

    staffList.value = rawList.map((item: any) => {
      const sName = item.surName || item.SurName || ''
      const fName = item.name || item.Name || ''
      const calculatedFullName = item.fullName || item.FullName || `${sName} ${fName}`.trim() || 'Сотрудник СДМТ'

      return {
        id: item.id ?? item.Id,
        name: fName,
        surName: sName,
        fullName: calculatedFullName,
        email: item.email || item.Email || '—',
        phoneNumber: item.phoneNumber || item.PhoneNumber || '—',
        role: item.role || item.Role || 'Dispatcher',
        lastEntranceAt:
          item.lastEntranceAt ||
          item.LastEntranceAt ||
          item.lastLoginAt ||
          item.LastLoginAt ||
          item.lastEntrance ||
          item.LastEntrance ||
          null,
        createdAt: item.createdAt || item.CreatedAt || null,
      }
    })
  } catch (err: any) {
    console.error('[ОРБИТА-ПЕРСОНАЛ] Ошибка загрузки:', err)
    showNotice(err.response?.data?.message || err.message || 'Сбой загрузки реестра сотрудников', true)
  } finally {
    isLoading.value = false
  }
}

// ==========================================
// 1. РЕГИСТРАЦИЯ СОТРУДНИКА
// ==========================================
const isRegisterDialogOpen = ref<boolean>(false)
const isRegistering = ref<boolean>(false)
const showRegisterPassword = ref<boolean>(false)
const isRegisterFormValid = ref<boolean>(false)

const registerForm = ref({
  name: '',
  surName: '',
  email: '',
  phoneNumber: '',
  role: 'Dispatcher',
  password: '',
})

// Автоформатирование телефона +7 (XXX) XXX-XX-XX
const onPhoneInput = (e: Event) => {
  const input = e.target as HTMLInputElement
  let digits = input.value.replace(/\D/g, '')

  if (digits.startsWith('7') || digits.startsWith('8')) {
    digits = digits.slice(1)
  }
  digits = digits.slice(0, 10)

  let formatted = '+7'
  if (digits.length > 0) formatted += ' (' + digits.slice(0, 3)
  if (digits.length >= 3) formatted += ') ' + digits.slice(3, 6)
  if (digits.length >= 6) formatted += '-' + digits.slice(6, 8)
  if (digits.length >= 8) formatted += '-' + digits.slice(8, 10)

  registerForm.value.phoneNumber = formatted
}

// Правила строгой валидации
const nameRules = [(v: string) => !!v?.trim() || 'Имя обязательно к заполнению']
const surNameRules = [(v: string) => !!v?.trim() || 'Фамилия обязательна к заполнению']
const emailRules = [
  (v: string) => !!v?.trim() || 'Email обязателен к заполнению',
  (v: string) =>
    /^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$/.test(v?.trim()) ||
    'Некорректный формат почты (например: operator@mtds.ru)',
]
const phoneRules = [
  (v: string) => !!v?.trim() || 'Телефон обязателен к заполнению',
  (v: string) =>
    /^\+7 \(\d{3}\) \d{3}-\d{2}-\d{2}$/.test(v?.trim()) ||
    'Введите полный номер: +7 (XXX) XXX-XX-XX',
]
const passwordRules = [
  (v: string) => !!v || 'Пароль обязателен к заполнению',
  (v: string) => (v && v.length >= 6) || 'Минимальная длина пароля — 6 символов',
]

// Кнопка активна только если все заполнено и валидно
const isRegisterSubmitAllowed = computed(() => {
  return (
    isRegisterFormValid.value &&
    registerForm.value.name.trim().length > 0 &&
    registerForm.value.surName.trim().length > 0 &&
    /^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$/.test(registerForm.value.email.trim()) &&
    /^\+7 \(\d{3}\) \d{3}-\d{2}-\d{2}$/.test(registerForm.value.phoneNumber.trim()) &&
    registerForm.value.password.length >= 6
  )
})

const openRegisterDialog = () => {
  registerForm.value = {
    name: '',
    surName: '',
    email: '',
    phoneNumber: '',
    role: 'Dispatcher',
    password: '',
  }
  showRegisterPassword.value = false
  isRegisterDialogOpen.value = true
}

const handleRegisterSubmit = async () => {
  if (!isRegisterSubmitAllowed.value) return

  try {
    isRegistering.value = true

    const payload = {
      name: registerForm.value.name.trim(),
      surName: registerForm.value.surName.trim(),
      email: registerForm.value.email.trim(),
      phoneNumber: registerForm.value.phoneNumber.trim(),
      role: registerForm.value.role,
      password: registerForm.value.password,
    }

    await apiClient.post('/auth/register', payload)

    showNotice(`Сотрудник ${payload.surName} ${payload.name} успешно зарегистрирован!`)

    registerForm.value.password = ''
    registerForm.value.name = ''
    registerForm.value.surName = ''
    registerForm.value.email = ''
    registerForm.value.phoneNumber = ''

    isRegisterDialogOpen.value = false
    await fetchStaff()
  } catch (err: any) {
    console.error('[ОРБИТА-ПЕРСОНАЛ] Ошибка регистрации:', err)
    showNotice(err.response?.data?.message || err.message || 'Ошибка регистрации сотрудника', true)
  } finally {
    isRegistering.value = false
  }
}

// ==========================================
// 2. СМЕНА ПАРОЛЯ СОТРУДНИКА
// ==========================================
const isPasswordDialogOpen = ref<boolean>(false)
const isChangingPassword = ref<boolean>(false)
const isPasswordFormValid = ref<boolean>(false)
const targetStaffMember = ref<StaffMember | null>(null)

const showNewPassword = ref<boolean>(false)
const showConfirmPassword = ref<boolean>(false)

const passwordForm = ref({
  newPassword: '',
  confirmPassword: '',
})

const newPasswordRules = [
  (v: string) => !!v || 'Введите новый пароль',
  (v: string) => (v && v.length >= 6) || 'Минимум 6 символов',
]

const confirmPasswordRules = [
  (v: string) => !!v || 'Повторите новый пароль',
  (v: string) => v === passwordForm.value.newPassword || 'Пароли не совпадают',
]

const isPasswordSubmitAllowed = computed(() => {
  return (
    isPasswordFormValid.value &&
    passwordForm.value.newPassword.length >= 6 &&
    passwordForm.value.newPassword === passwordForm.value.confirmPassword
  )
})

const openChangePasswordDialog = (staff: StaffMember) => {
  targetStaffMember.value = staff
  passwordForm.value = {
    newPassword: '',
    confirmPassword: '',
  }
  showNewPassword.value = false
  showConfirmPassword.value = false
  isPasswordDialogOpen.value = true
}

const handleChangePasswordSubmit = async () => {
  if (!isPasswordSubmitAllowed.value || !targetStaffMember.value) return

  try {
    isChangingPassword.value = true

    await apiClient.patch(`/auth/staff/${targetStaffMember.value.id}/password`, {
      newPassword: passwordForm.value.newPassword,
    })

    showNotice('Пароль успешно изменен')

    passwordForm.value.newPassword = ''
    passwordForm.value.confirmPassword = ''
    isPasswordDialogOpen.value = false
  } catch (err: any) {
    console.error('[ОРБИТА-ПЕРСОНАЛ] Ошибка смены пароля:', err)
    showNotice(err.response?.data?.message || err.message || 'Не удалось обновить пароль сотрудника', true)
  } finally {
    isChangingPassword.value = false
  }
}

// ==========================================
// 3. УДАЛЕНИЕ СОТРУДНИКА
// ==========================================
const deleteDialog = ref({
  open: false,
  loading: false,
  target: null as StaffMember | null,
})

const openDeleteStaffDialog = (staff: StaffMember) => {
  if (isSelf(staff)) return
  deleteDialog.value = {
    open: true,
    loading: false,
    target: staff,
  }
}

const handleConfirmDelete = async () => {
  if (!deleteDialog.value.target) return

  try {
    deleteDialog.value.loading = true
    const targetId = deleteDialog.value.target.id

    await apiClient.delete(`/auth/staff/${targetId}`)

    showNotice(`Сотрудник ${deleteDialog.value.target.fullName} успешно удален из системы`)
    deleteDialog.value.open = false
    await fetchStaff()
  } catch (err: any) {
    console.error('[ОРБИТА-ПЕРСОНАЛ] Ошибка удаления:', err)
    showNotice(err.response?.data?.message || err.message || 'Не удалось удалить сотрудника', true)
  } finally {
    deleteDialog.value.loading = false
  }
}

onMounted(() => {
  fetchStaff()
})
</script>

<template>
  <div class="staff-admin-view pa-6">
    <!-- Шапка страницы -->
    <div class="mb-4">
      <div class="d-flex align-center">
        <v-icon icon="mdi-shield-account" color="#00E676" size="26" class="mr-2" />
        <h1 class="text-h6 font-weight-black tracking-wider brand-title mb-0">
          УПРАВЛЕНИЕ ПЕРСОНАЛОМ
        </h1>
      </div>
      <p class="text-caption text-disabled font-mono mt-1 mb-0">
        УЧЕТ ДИСПЕТЧЕРОВ, АДМИНИСТРАТОРОВ И ПОЛИТИКА АУТЕНТИФИКАЦИИ СДМТ
      </p>
    </div>

    <!-- Тулбар: Поиск + Кнопка обновить вплотную справа + Кнопка добавления -->
    <div class="d-flex align-center justify-space-between mb-3">
      <div class="d-flex align-center" style="max-width: 440px; width: 100%;">
        <v-text-field
          v-model="search"
          prepend-inner-icon="mdi-magnify"
          placeholder="Поиск по ФИО, Email, телефону..."
          density="compact"
          variant="outlined"
          color="#00E676"
          hide-details
          class="font-mono search-input mr-2"
          autocomplete="off"
          autocorrect="off"
          spellcheck="false"
        />

        <!-- Кнопка обновления рядом с поиском -->
        <v-tooltip text="Обновить реестр сотрудников" location="top">
          <template #activator="{ props }">
            <v-btn
              v-bind="props"
              icon="mdi-refresh"
              variant="outlined"
              size="small"
              rounded="0"
              :loading="isLoading"
              class="border-subtle"
              @click="fetchStaff"
            />
          </template>
        </v-tooltip>
      </div>

      <v-btn
        color="#00E676"
        variant="flat"
        rounded="0"
        size="small"
        prepend-icon="mdi-account-plus"
        class="font-mono font-weight-bold text-black"
        @click="openRegisterDialog"
      >
        ДОБАВИТЬ СОТРУДНИКА
      </v-btn>
    </div>

    <!-- Таблица сотрудников -->
    <v-card class="table-card" rounded="0">
      <v-data-table
        :headers="headers"
        :items="staffList"
        :search="search"
        :loading="isLoading"
        density="compact"
        class="custom-table font-mono"
        items-per-page-text="Строк на странице:"
        no-data-text="Сотрудники не найдены"
      >
        <template #item.fullName="{ item, value }">
          <div class="d-flex align-center">
            <span class="font-weight-bold text-white">{{ value }}</span>
            <v-chip
              v-if="isSelf(item as any)"
              size="x-small"
              color="#00E676"
              variant="tonal"
              class="ml-2 font-mono"
              style="font-size: 9px !important; height: 18px;"
            >
              ВЫ
            </v-chip>
          </div>
        </template>

        <template #item.email="{ value }">
          <span class="text-grey-lighten-1">{{ value }}</span>
        </template>

        <template #item.phoneNumber="{ value }">
          <span class="text-grey-lighten-2">{{ value }}</span>
        </template>

        <!-- Роль доступа: цветной бейдж -->
        <template #item.role="{ value }">
          <v-chip
            size="x-small"
            label
            class="font-weight-bold role-chip"
            :style="{
              color: value === 'Admin' ? '#FF1744' : '#29B6F6',
              borderColor: value === 'Admin' ? '#FF1744' : '#29B6F6',
              backgroundColor: value === 'Admin' ? 'rgba(255, 23, 68, 0.08)' : 'rgba(41, 182, 246, 0.08)',
            }"
            variant="outlined"
          >
            ● {{ value === 'Admin' ? 'АДМИНИСТРАТОР' : 'ДИСПЕТЧЕР' }}
          </v-chip>
        </template>

        <!-- Время последнего входа -->
        <template #item.lastEntranceAt="{ item, value }">
          <span
            :class="
              formatDateTime((item as any).lastEntranceAt ?? (item as any).raw?.lastEntranceAt ?? value) !== 'Никогда'
                ? 'text-grey-lighten-2': 'text-disabled'
            "
          >
            {{ formatDateTime((item as any).lastEntranceAt ?? (item as any).raw?.lastEntranceAt ?? value) }}
          </span>
        </template>

        <!-- Действия -->
        <template #item.actions="{ item }">
          <div class="d-flex align-center justify-center">
            <!-- Смена пароля -->
            <v-tooltip text="Сменить пароль" location="top">
              <template #activator="{ props }">
                <v-btn
                  v-bind="props"
                  icon="mdi-key-variant"
                  size="x-small"
                  variant="text"
                  color="#FFD600"
                  class="mr-1"
                  @click="openChangePasswordDialog(item as any)"
                />
              </template>
            </v-tooltip>

            <!-- Удаление сотрудника (заблокировано для самого себя) -->
            <v-tooltip
              :text="isSelf(item as any) ? 'Нельзя удалить собственный профиль' : 'Удалить сотрудника'"
              location="top"
            >
              <template #activator="{ props }">
                <span v-bind="props" class="d-inline-block">
                  <v-btn
                    icon="mdi-delete"
                    size="x-small"
                    variant="text"
                    color="#FF1744"
                    :disabled="isSelf(item as any)"
                    @click="openDeleteStaffDialog(item as any)"
                  />
                </span>
              </template>
            </v-tooltip>
          </div>
        </template>
      </v-data-table>
    </v-card>

    <!-- ДИАЛОГ 1: РЕГИСТРАЦИЯ СОТРУДНИКА (С ПРОВЕРКОЙ И PERSISTENT) -->
    <v-dialog v-model="isRegisterDialogOpen" max-width="520" persistent transition="fade-transition">
      <v-card class="admin-dialog-card pa-4" rounded="0">
        <div class="d-flex align-center justify-space-between mb-2">
          <div class="text-subtitle-2 font-mono font-weight-bold text-white tracking-wider">
            РЕГИСТРАЦИЯ ОПЕРАТОРА СДМТ
          </div>
          <v-btn
            icon="mdi-close"
            variant="text"
            size="x-small"
            rounded="0"
            :disabled="isRegistering"
            @click="isRegisterDialogOpen = false"
          />
        </div>
        <v-divider class="border-opacity-10 mb-4" />

        <v-form
          ref="registerFormRef"
          v-model="isRegisterFormValid"
          autocomplete="off"
          @submit.prevent="handleRegisterSubmit"
        >
          <v-row class="mb-1">
            <v-col cols="6">
              <div class="text-caption text-disabled font-mono mb-1">ИМЯ *</div>
              <v-text-field
                v-model="registerForm.name"
                placeholder="Дмитрий"
                variant="outlined"
                density="compact"
                color="#00E676"
                class="font-mono input-tactical"
                autocomplete="off"
                :rules="nameRules"
              />
            </v-col>
            <v-col cols="6">
              <div class="text-caption text-disabled font-mono mb-1">ФАМИЛИЯ *</div>
              <v-text-field
                v-model="registerForm.surName"
                placeholder="Алексеев"
                variant="outlined"
                density="compact"
                color="#00E676"
                class="font-mono input-tactical"
                autocomplete="off"
                :rules="surNameRules"
              />
            </v-col>
          </v-row>

          <div class="text-caption text-disabled font-mono mb-1">СЛУЖЕБНЫЙ EMAIL *</div>
          <v-text-field
            v-model="registerForm.email"
            placeholder="dispatcher@mtds.ru"
            variant="outlined"
            density="compact"
            color="#00E676"
            class="font-mono mb-1 input-tactical"
            autocomplete="off"
            :rules="emailRules"
          /><v-row class="mb-1">
            <v-col cols="6">
              <div class="text-caption text-disabled font-mono mb-1">ТЕЛЕФОН СВЯЗИ *</div>
              <v-text-field
                :model-value="registerForm.phoneNumber"
                placeholder="+7 (999) 000-00-00"
                variant="outlined"
                density="compact"
                color="#00E676"
                class="font-mono input-tactical"
                autocomplete="off"
                :rules="phoneRules"
                @input="onPhoneInput"
              />
            </v-col>
            <v-col cols="6">
              <div class="text-caption text-disabled font-mono mb-1">РОЛЬ ДОСТУПА *</div>
              <v-select
                v-model="registerForm.role"
                :items="roleSelectOptions"
                item-title="title"
                item-value="value"
                variant="outlined"
                density="compact"
                color="#00E676"
                class="font-mono input-tactical"
                hide-details
              />
            </v-col>
          </v-row>

          <div class="text-caption text-disabled font-mono mb-1 mt-1">
            ВРЕМЕННЫЙ ПАРОЛЬ (МИНИМУМ 6 СИМВОЛОВ) *
          </div>
          <v-text-field
            v-model="registerForm.password"
            :type="showRegisterPassword ? 'text' : 'password'"
            placeholder="••••••••••••"
            :append-inner-icon="showRegisterPassword ? 'mdi-eye-off-outline' : 'mdi-eye-outline'"
            variant="outlined"
            density="compact"
            color="#00E676"
            class="font-mono mb-4 input-tactical"
            autocomplete="new-password"
            :rules="passwordRules"
            @click:append-inner="showRegisterPassword = !showRegisterPassword"
          />

          <div class="d-flex justify-end">
            <v-btn
              variant="text"
              size="small"
              rounded="0"
              class="font-mono mr-2"
              :disabled="isRegistering"
              @click="isRegisterDialogOpen = false"
            >
              ОТМЕНА
            </v-btn>
            <v-btn
              type="submit"
              color="#00E676"
              variant="flat"
              size="small"
              rounded="0"
              class="font-mono font-weight-bold text-black px-4"
              :loading="isRegistering"
              :disabled="!isRegisterSubmitAllowed"
            >
              ЗАРЕГИСТРИРОВАТЬ
            </v-btn>
          </div>
        </v-form>
      </v-card>
    </v-dialog>

    <!-- ДИАЛОГ 2: СМЕНА ПАРОЛЯ СОТРУДНИКА (PERSISTENT) -->
    <v-dialog v-model="isPasswordDialogOpen" max-width="460" persistent transition="fade-transition">
      <v-card class="admin-dialog-card pa-4" rounded="0">
        <div class="d-flex align-center justify-space-between mb-2">
          <div class="d-flex align-center">
            <v-icon icon="mdi-key-variant" color="#FFD600" size="20" class="mr-2" />
            <span class="text-subtitle-2 font-mono font-weight-bold text-white tracking-wider">
              СМЕНА ПАРОЛЯ ОПЕРАТОРА
            </span>
          </div>
          <v-btn
            icon="mdi-close"
            variant="text"
            size="x-small"
            rounded="0"
            :disabled="isChangingPassword"
            @click="isPasswordDialogOpen = false"
          />
        </div>
        <v-divider class="border-opacity-10 mb-3" />

        <div class="employee-info-box pa-3 mb-4 font-mono">
          <div class="text-caption text-disabled">СОТРУДНИК:</div>
          <div class="text-body-2 font-weight-bold text-white line-clamp-1">
            {{ targetStaffMember?.fullName }}
          </div>
          <div class="text-caption text-grey-lighten-1 mt-1">
            {{ targetStaffMember?.email }}
          </div>
        </div>

        <v-form v-model="isPasswordFormValid" autocomplete="off" @submit.prevent="handleChangePasswordSubmit">
          <div class="text-caption text-disabled font-mono mb-1">
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
            class="font-mono mb-2 input-tactical"
            autocomplete="new-password"
            :rules="newPasswordRules"
            @click:append-inner="showNewPassword = !showNewPassword"
          />

          <div class="text-caption text-disabled font-mono mb-1">
            ПОВТОРИТЕ НОВЫЙ ПАРОЛЬ *
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
            :rules="confirmPasswordRules"
            @click:append-inner="showConfirmPassword = !showConfirmPassword"
          />

          <div class="d-flex justify-end">
            <v-btn
              variant="text"
              size="small"
              rounded="0"
              class="font-mono mr-2"
              :disabled="isChangingPassword"
              @click="isPasswordDialogOpen = false"
            >
              ОТМЕНА
            </v-btn>
            <v-btn
              type="submit"
              color="#00E676"
              variant="flat"
              size="small"
              rounded="0"
              class="font-mono font-weight-bold text-black px-4"
              :loading="isChangingPassword"
              :disabled="!isPasswordSubmitAllowed"
            >
              СОХРАНИТЬ ПАРОЛЬ
            </v-btn>
          </div>
        </v-form>
      </v-card>
    </v-dialog>

    <!-- ДИАЛОГ 3: ПОДТВЕРЖДЕНИЕ УДАЛЕНИЯ СОТРУДНИКА (PERSISTENT) -->
    <v-dialog v-model="deleteDialog.open" max-width="450" persistent transition="fade-transition">
      <v-card class="admin-dialog-card pa-5" rounded="0">
        <div class="d-flex align-center mb-3">
          <v-icon icon="mdi-alert-octagon-outline" color="#FF1744" size="26" class="mr-2" />
          <div class="text-subtitle-2 font-weight-bold font-mono text-white">
            ПОДТВЕРЖДЕНИЕ УДАЛЕНИЯ
          </div>
        </div>

        <v-divider class="border-opacity-10 mb-4" />

        <div class="text-body-2 font-mono text-grey-lighten-2 mb-2">
          Вы уверены, что хотите отозвать доступ и удалить учетную запись сотрудника:
        </div>

        <div class="employee-info-box pa-3 mb-4 font-mono">
          <div class="text-body-2 font-weight-bold text-red-accent-2">
            {{ deleteDialog.target?.fullName }}
          </div>
          <div class="text-caption text-disabled mt-1">
            Email: {{ deleteDialog.target?.email }} • Роль:
            {{ deleteDialog.target?.role === 'Admin' ? 'Администратор' : 'Диспетчер' }}
          </div>
        </div>

        <div class="d-flex justify-end">
          <v-btn
            variant="text"
            size="small"
            rounded="0"
            class="font-mono mr-2"
            :disabled="deleteDialog.loading"
            @click="deleteDialog.open = false"
          >
            ОТМЕНА
          </v-btn>
          <v-btn
            color="#FF1744"
            variant="flat"
            size="small"
            rounded="0"
            class="font-mono font-weight-bold text-white px-4"
            :loading="deleteDialog.loading"
            @click="handleConfirmDelete"
          >
            УДАЛИТЬ СОТРУДНИКА
          </v-btn>
        </div>
      </v-card>
    </v-dialog>

    <!-- Снэкбар -->
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
.staff-admin-view {
  height: 100%;
  overflow-y: auto;
  background-color: #0A0C10;
}
.brand-title {
  letter-spacing: 0.12em;
  color: #F0F6FC;
}
.table-card {
  background-color: #161B22 !important;
  border: 1px solid #30363D !important;
}
.search-input,
.input-tactical {
  background-color: #0E1116;
}
.border-subtle {
  border-color: #30363D !important;
}
.role-chip {
  letter-spacing: 0.05em;
  font-size: 10px !important;
}
.employee-info-box {
  background-color: #0E1116;
  border: 1px solid #21262D;
}
.admin-dialog-card {
  background-color: #161B22 !important;
  border: 1px solid #30363D !important;
  box-shadow: 0 0 28px rgba(0, 0, 0, 0.7) !important;
}

:deep(.v-data-table) {
  background-color: transparent !important;
  color: #C9D1D9 !important;
}
:deep(.v-data-table-header th) {
  background-color: #0E1116 !important;
  color: #8B949E !important;
  font-size: 11px !important;
  border-bottom: 1px solid #30363D !important;
}
:deep(td) {
  border-bottom: 1px solid #21262D !important;
  font-size: 13px !important;
}
.font-mono {
  font-family: 'JetBrains Mono', Consolas, monospace;
}
</style>