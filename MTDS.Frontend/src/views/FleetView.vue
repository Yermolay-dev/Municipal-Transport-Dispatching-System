<script setup lang="ts">
import { ref, onMounted } from 'vue'
import apiClient from '../api'

interface TransportUnit {
  id: string | number
  numberPlate: string
  capacity: number
  status: string
  type: string
}

const search = ref<string>('')
const isLoading = ref<boolean>(false)
const fleetData = ref<TransportUnit[]>([])
const errorMessage = ref<string | null>(null)

// Модальное окно экстренной команды
const isEmergencyDialogOpen = ref<boolean>(false)
const isSendingCommand = ref<boolean>(false)
const selectedTransport = ref<TransportUnit | null>(null)
const selectedCommand = ref<string>('STOP')
const commandReason = ref<string>('')

// Уведомления (Snackbar)
const snackbar = ref({
  show: false,
  text: '',
  color: '#00E676',
})

// Доступные типы экстренных команд и их русские наименования
const commandOptions = [
  { title: 'Экстренная остановка (блокировка)', value: 'STOP' },
  { title: 'Возврат в парк (сход с линии)', value: 'RETURN_TO_PARK' },
  { title: 'Срочный вызов по рации', value: 'RADIO_CALL_REQUIRED' },
  { title: 'Объезд препятствия (смена маршрута)', value: 'ROUTE_DETOUR' },
  { title: 'Принудительная разблокировка дверей', value: 'DOORS_UNLOCK' },
  { title: 'Подтверждение тихой тревоги', value: 'SILENT_ALARM_ACK' },
]

// Конфигурация колонок таблицы
const headers = [
  { title: 'ID БОРТА', key: 'id', align: 'start' as const },
  { title: 'ГОСНОМЕР', key: 'numberPlate', align: 'start' as const },
  { title: 'ТИП', key: 'type', align: 'start' as const },
  { title: 'ВМЕСТИМОСТЬ', key: 'capacity', align: 'center' as const },
  { title: 'СТАТУС', key: 'status', align: 'center' as const },
  { title: 'ДИСПЕТЧЕРИЗАЦИЯ', key: 'actions', align: 'center' as const, sortable: false },
]

// Словарь статусов на русском языке со строгими диспетчерскими цветами
const statusDictionary: Record<string, { text: string; color: string }> = {
  active: { text: 'На линии', color: '#00E676' },
  on_route: { text: 'На линии', color: '#00E676' },
  in_garage: { text: 'В парке', color: '#8B949E' },
  garage: { text: 'В парке', color: '#8B949E' },
  emergency_stopped: { text: 'Аварийная остановка', color: '#FF1744' },
  stop: { text: 'Аварийная остановка', color: '#FF1744' },
  return_to_park: { text: 'Возврат в парк', color: '#FFD600' },
  returning_to_park: { text: 'Возврат в парк', color: '#FFD600' },
  radio_call_required: { text: 'Срочная радиосвязь', color: '#FF9100' },
  route_detour: { text: 'Объезд маршрута', color: '#29B6F6' },
  route_dotour: { text: 'Объезд маршрута', color: '#29B6F6' },
  doors_unlock: { text: 'Разблокировка дверей', color: '#FF1744' },
  silent_alarm_ack: { text: 'Тихая тревога', color: '#FF1744' },
  silent_alarm: { text: 'Тихая тревога', color: '#FF1744' },
}

// Преобразование любого входящего статуса в чистый русский текст
const getStatusInfo = (rawStatus: string): { text: string; color: string } => {
  const norm = (rawStatus || '').toLowerCase().replace(/[\s-]+/g, '_').trim()

  if (statusDictionary[norm]) {
    return statusDictionary[norm]
  }

  // Нечеткий поиск (fuzzy)
  if (norm.includes('garage') || norm.includes('парк')) return { text: 'В парке', color: '#8B949E' }
  if (norm.includes('active') || norm.includes('лини')) return { text: 'На линии', color: '#00E676' }
  if (norm.includes('emergency') || norm.includes('авар') || norm === 'stop') return { text: 'Аварийная остановка', color: '#FF1744' }
  if (norm.includes('return')) return { text: 'Возврат в парк', color: '#FFD600' }
  if (norm.includes('radio')) return { text: 'Срочная радиосвязь', color: '#FF9100' }
  if (norm.includes('detour') || norm.includes('dotour') || norm.includes('объезд')) return { text: 'Объезд маршрута', color: '#29B6F6' }
  if (norm.includes('door') ) return { text: 'Разблокировка дверей', color: '#FF1744' }
  if (norm.includes('alarm') || norm.includes('тревог')) return { text: 'Тихая тревога', color: '#FF1744' }

  // Очистка от нижних подчеркиваний
  const clean = (rawStatus || 'Неизвестно').replace(/_/g, ' ')
  return {
    text: clean.charAt(0).toUpperCase() + clean.slice(1),
    color: '#FFD600',
  }
}

// Доступность кнопки: бортам в парке команды не отдаются, остальным — доступны
const isCommandAllowed = (status: string): boolean => {
  const norm = (status || '').toLowerCase().replace(/[\s-]+/g, '_').trim()
  return !norm.includes('garage') && !norm.includes('парк')
}

// Загрузка подвижного состава
const fetchFleet = async () => {
  try {
    isLoading.value = true
    errorMessage.value = null

    const response = await apiClient.get('/park')

    let rawList: any[] = []
    if (Array.isArray(response.data)) {
      rawList = response.data
    } else if (response.data && Array.isArray(response.data.park)) {
      rawList = response.data.park
    } else if (response.data && Array.isArray(response.data.Park)) {
      rawList = response.data.Park
    } else if (response.data && Array.isArray(response.data.data)) {
      rawList = response.data.data
    }

    fleetData.value = rawList.map((item: any) => ({
      id: item.id ?? item.Id ?? '—',
      numberPlate: item.numberPlate || item.NumberPlate || 'Б/Н',
      capacity: item.capacity ?? item.Capacity ?? 0,
      status: item.status || item.Status || 'in_garage',
      type: item.type || item.Type || 'bus',
    }))
  } catch (err: any) {
    console.error('[ОРБИТА-ПАРК] Ошибка загрузки парка:', err)
    errorMessage.value = err.message || 'Сбой опроса реестра подвижного состава'
  } finally {
    isLoading.value = false
  }
}

// Открытие диалога экстренной команды
const openEmergencyDialog = (unit: any) => {
  const rawItem = unit.raw || unit
  if (!isCommandAllowed(rawItem.status)) return

  selectedTransport.value = {
    id: rawItem.id,
    numberPlate: rawItem.numberPlate,
    capacity: rawItem.capacity,
    status: rawItem.status,
    type: rawItem.type,
  }
  selectedCommand.value = 'STOP'
  commandReason.value = ''
  isEmergencyDialogOpen.value = true
}

const closeEmergencyDialog = () => {
  isEmergencyDialogOpen.value = false
  selectedTransport.value = null
  commandReason.value = ''
}

// Отправка команды
const handleSendEmergencyCommand = async () => {
  if (!commandReason.value || commandReason.value.trim().length < 10) {
    return
  }

  if (!selectedTransport.value) return

  try {
    isSendingCommand.value = true

    const commandItem = commandOptions.find((o) => o.value === selectedCommand.value)
    const titleRu = commandItem ? commandItem.title : selectedCommand.value

    // Отправляем расширенный payload, чтобы C# бэкенд нашел нужное поле
    const payload = {
      transportId: Number(selectedTransport.value.id),
      // Коды
      actionType: selectedCommand.value,
      action: selectedCommand.value,
      command: selectedCommand.value,
      // Русские названия команды (чтобы бэкенд мог сразу записать в лог)
      commandTitle: titleRu,
      actionTitle: titleRu,
      commandName: titleRu,
      // Причина
      reason: commandReason.value.trim(),
      comment: commandReason.value.trim(),
    }

    console.log('[ОРБИТА-ЭКСТРЕННО] Отправка приказа:', payload)
    const response = await apiClient.post('/emergency/command', payload)

    closeEmergencyDialog()

    const serverMsg = response.data?.message || response.data?.Message
    snackbar.value = {
      show: true,
      text: serverMsg || `Приказ "${titleRu}" передан на борт ${selectedTransport.value.numberPlate}`,
      color: '#00E676',
    }

    // Автоматическое обновление таблицы
    await fetchFleet()
  } catch (err: any) {
    console.error('[ОРБИТА-ЭКСТРЕННО] Ошибка передачи команды:', err)
    const errText =
      err.response?.data?.message ||
      err.response?.data?.title ||
      err.message ||
      'Ошибка отправки экстренной команды'
    snackbar.value = {
      show: true,
      text: `СБОЙ ПЕРЕДАЧИ: ${errText}`,
      color: '#FF1744',
    }
  } finally {
    isSendingCommand.value = false
  }
}

onMounted(() => {
  fetchFleet()
})
</script>

<template>
  <div class="fleet-view pa-6">
    <!-- Шапка -->
    <div class="d-flex align-center justify-space-between mb-4">
      <div>
        <h1 class="text-h6 font-weight-black tracking-wider brand-title mb-1">
          РЕЕСТР ПОДВИЖНОГО СОСТАВА
        </h1>
        <p class="text-caption text-disabled font-mono">
          МОНИТОРИНГ, УЧЕТ И ОПЕРАТИВНОЕ УПРАВЛЕНИЕ ТРАНСПОРТОМ
        </p>
      </div>

      <div class="d-flex align-center">
        <v-text-field
          v-model="search"
          prepend-inner-icon="mdi-magnify"
          placeholder="Поиск по номеру, ID..."
          density="compact"
          variant="outlined"
          color="#00E676"
          hide-details
          class="font-mono search-input mr-3"
          style="width: 280px;"
        />
        <v-btn
          icon="mdi-refresh"
          variant="outlined"
          size="small"
          rounded="0"
          :loading="isLoading"
          class="border-subtle"
          @click="fetchFleet"
        />
      </div>
    </div>

    <!-- Таблица транспорта -->
    <v-card class="table-card" rounded="0">
      <v-data-table
        :headers="headers"
        :items="fleetData"
        :search="search"
        :loading="isLoading"
        density="compact"
        class="custom-table font-mono"
        items-per-page-text="Строк на странице:"
        no-data-text="Транспортные средства не обнаружены"
      >
        <template #item.id="{ item, value }">
          <span class="text-grey-lighten-1 font-weight-bold">
            #{{ value ?? (item as any).id ?? (item as any).raw?.id }}
          </span>
        </template>

        <template #item.numberPlate="{ item, value }">
          <span class="number-plate-badge">
            {{ value ?? (item as any).numberPlate ?? (item as any).raw?.numberPlate }}
          </span>
        </template>

        <template #item.type="{ item, value }">
          <span class="text-capitalize">
            {{ value ?? (item as any).type ?? (item as any).raw?.type }}
          </span>
        </template>

        <template #item.capacity="{ item, value }">
          <span>{{ value ?? (item as any).capacity ?? (item as any).raw?.capacity }} чел.</span>
        </template>

        <!-- Чип статуса: полностью на русском языке и без подчеркиваний -->
        <template #item.status="{ item, value }">
          <v-chip
            size="x-small"
            label
            class="font-weight-bold status-chip"
            :style="{
              color: getStatusInfo(value ?? (item as any).status ?? (item as any).raw?.status).color,
              borderColor: getStatusInfo(value ?? (item as any).status ?? (item as any).raw?.status).color,
              backgroundColor: 'rgba(255, 255, 255, 0.03)'
            }"
            variant="outlined"
          >
            ● {{ getStatusInfo(value ?? (item as any).status ?? (item as any).raw?.status).text }}
          </v-chip>
        </template>

        <!-- Кнопка действия -->
        <template #item.actions="{ item }">
          <v-tooltip
            :text="isCommandAllowed((item as any).status ?? (item as any).raw?.status) ? 'Открыть канал подачи экстренной команды' : 'Борт находится в парке (подача команд заблокирована)'"
            location="top"
          >
            <template #activator="{ props }">
              <span v-bind="props" class="d-inline-block">
                <v-btn
                  size="small"
                  variant="flat"
                  rounded="0"
                  density="compact"
                  :disabled="!isCommandAllowed((item as any).status ?? (item as any).raw?.status)"
                  :class="[
                    'command-action-btn',
                    isCommandAllowed((item as any).status ?? (item as any).raw?.status) ? 'command-action-btn-active' : 'command-action-btn-disabled'
                  ]"
                  @click="openEmergencyDialog(item)"
                >
                  <v-icon icon="mdi-radio-handheld" size="15" class="mr-1" />
                  <span class="font-weight-bold tracking-wider" style="font-size: 11px;">ПРИКАЗ</span>
                </v-btn>
              </span>
            </template>
          </v-tooltip>
        </template>
      </v-data-table>
    </v-card>

    <v-alert v-if="errorMessage" type="error" variant="tonal" class="mt-4" rounded="0">
      <span class="font-mono text-caption">{{ errorMessage }}</span>
    </v-alert>

    <!-- МОДАЛЬНОЕ ОКНО: ПЛАВНЫЙ FADE -->
    <v-dialog
      v-model="isEmergencyDialogOpen"
      max-width="520"
      persistent
      transition="fade-transition"
    >
      <v-card class="emergency-dialog-card" rounded="0">
        <div class="emergency-header pa-4 d-flex align-center justify-space-between">
          <div class="d-flex align-center">
            <div class="pulsing-radar-box mr-3">
              <v-icon icon="mdi-radio-handheld" color="#FF1744" size="20" />
            </div>
            <div>
              <div class="text-subtitle-2 font-weight-black font-mono text-white tracking-wider">
                ОПЕРАТИВНЫЙ ПРИКАЗ ДИСПЕТЧЕРА
              </div>
              <div class="text-caption text-grey-lighten-1 font-mono" style="font-size: 11px !important;">
                ЦЕЛЬ: БОРТ #{{ selectedTransport?.id }} • [{{ selectedTransport?.numberPlate }}]
              </div>
            </div>
          </div>
          <v-btn
            icon="mdi-close"
            variant="text"
            size="small"
            rounded="0"
            :disabled="isSendingCommand"
            @click="closeEmergencyDialog"
          />
        </div>

        <v-divider class="border-opacity-20" />

        <v-card-text class="pa-5">
          <div class="text-caption text-grey-lighten-1 font-mono mb-1">
            ТИП КОМАНДЫ ДЛЯ БОРТОВОГО КОМПЬЮТЕРА
          </div>
          <v-select
            v-model="selectedCommand"
            :items="commandOptions"
            item-title="title"
            item-value="value"
            variant="outlined"
            density="compact"
            color="#FF1744"
            rounded="0"
            class="font-mono mb-4 input-tactical"
            hide-details
          />

          <div class="text-caption text-grey-lighten-1 font-mono mb-1">
            ПРИЧИНА / ОБОСНОВАНИЕ ДЛЯ БОРТОВОГО ЖУРНАЛА *
          </div>
          <v-textarea
            v-model="commandReason"
            rows="3"
            variant="outlined"
            density="compact"
            color="#FF1744"
            counter="250"
            persistent-hint
            hint="Опишите причину детально (минимум 10 символов)"
            placeholder="Например: ДТП на перекрёстке ул. Ленина, блокировка проезда..."
            rounded="0"
            class="font-mono input-tactical"
            :rules="[
              (v: string) => !!v?.trim() || 'Причина команды обязательна к заполнению',
              (v: string) => (v && v.trim().length >= 10) || 'Минимальная длина обоснования — 10 символов'
            ]"
          />
        </v-card-text>

        <v-divider class="border-opacity-20" />

        <v-card-actions class="pa-4 d-flex justify-end">
          <v-btn
            variant="text"
            rounded="0"
            size="small"
            class="font-mono mr-2"
            :disabled="isSendingCommand"
            @click="closeEmergencyDialog"
          >
            ОТМЕНА
          </v-btn>

          <v-btn
            color="#FF1744"
            variant="flat"
            rounded="0"
            size="small"
            class="font-mono font-weight-bold text-white px-4"
            :loading="isSendingCommand"
            :disabled="!commandReason || commandReason.trim().length < 10"
            @click="handleSendEmergencyCommand"
          >
            ПЕРЕДАТЬ ПРИКАЗ
          </v-btn>
        </v-card-actions>
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
.fleet-view {
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
.search-input {
  background-color: #0E1116;
}
.border-subtle {
  border-color: #30363D !important;
}
.number-plate-badge {
  background-color: #21262D;
  padding: 2px 8px;
  border: 1px solid #30363D;
  border-radius: 2px;
  color: #F0F6FC;
  font-weight: bold;
}
.status-chip {
  letter-spacing: 0.04em;
}

/* Кнопка приказа */
.command-action-btn {
  height: 28px !important;
  padding: 0 10px !important;
  letter-spacing: 0.06em;
}
.command-action-btn-active {
  background-color: rgba(255, 23, 68, 0.15) !important;
  border: 1px solid #FF1744 !important;
  color: #FF1744 !important;
  transition: all 0.2s ease;
}
.command-action-btn-active:hover {
  background-color: #FF1744 !important;
  color: #FFFFFF !important;
  box-shadow: 0 0 12px rgba(255, 23, 68, 0.5);
}
.command-action-btn-disabled {
  background-color: rgba(255, 255, 255, 0.03) !important;
  border: 1px solid #30363D !important;
  color: #6E7681 !important;
  opacity: 0.5;
}

/* Диалог */
.emergency-dialog-card {
  background-color: #161B22 !important;
  border: 1px solid #FF1744 !important;
  box-shadow: 0 0 30px rgba(255, 23, 68, 0.2) !important;
}
.emergency-header {
  background-color: #0E1116;
}
.input-tactical {
  background-color: #0E1116;
}
.pulsing-radar-box {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  background-color: rgba(255, 23, 68, 0.12);
  border: 1px solid rgba(255, 23, 68, 0.4);
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