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

const headers = [
  { title: 'ИД БОРТА', key: 'id', align: 'start' as const },
  { title: 'ГОСНОМЕР', key: 'numberPlate', align: 'start' as const },
  { title: 'ТИП', key: 'type', align: 'start' as const },
  { title: 'ВМЕСТИМОСТЬ', key: 'capacity', align: 'center' as const },
  { title: 'СТАТУС ТЕЛЕМЕТРИИ', key: 'status', align: 'center' as const },
]

const getStatusColor = (status: string) => {
  const norm = (status || '').toLowerCase()
  if (norm === 'active' || norm === 'в рейсе') return '#00E676'
  if (norm.includes('emergency') || norm === 'emergency_stopped') return '#FF1744'
  if (norm.includes('garage') || norm === 'in_garage') return '#8B949E'
  return '#FFD600'
}

const getStatusText = (status: string) => {
  const norm = (status || '').toUpperCase()
  if (norm === 'ACTIVE') return 'НА ЛИНИИ'
  if (norm === 'EMERGENCY_STOPPED') return 'АВАРИЙНАЯ ОСТАНОВКА'
  if (norm === 'IN_GARAGE') return 'В ПАРКЕ'
  return norm || 'НЕИЗВЕСТНО'
}

const fetchFleet = async () => {
  try {
    isLoading.value = true
    errorMessage.value = null

    const response = await apiClient.get('/park')
    console.log('[ОРБИТА-ПАРК] Ответ сервера /park:', response.data)

    // Извлекаем массив независимо от возможной обертки
// Извлекаем массив с учетом поля Park / park
    let rawList: any[] = []
    if (Array.isArray(response.data)) {
      rawList = response.data
    } else if (response.data && Array.isArray(response.data.park)) {
      rawList = response.data.park // <-- вот ключ C#
    } else if (response.data && Array.isArray(response.data.Park)) {
      rawList = response.data.Park
    } else if (response.data && Array.isArray(response.data.data)) {
      rawList = response.data.data
    }
    // Нормализация: приводим ключи из PascalCase/camelCase к единому формату
    fleetData.value = rawList.map((item: any) => ({
      id: item.id ?? item.Id ?? '—',
      numberPlate: item.numberPlate || item.NumberPlate || 'Б/Н',
      capacity: item.capacity ?? item.Capacity ?? 0,
      status: item.status || item.Status || 'in_garage',
      type: item.type || item.Type || 'bus',
    }))

    console.log('[ОРБИТА-ПАРК] Нормализовано записей для таблицы:', fleetData.value.length)
  } catch (err: any) {
    console.error('[ОРБИТА-ПАРК] Ошибка загрузки парка:', err)
    errorMessage.value = err.message || 'Сбой опроса реестра подвижного состава'
  } finally {
    isLoading.value = false
  }
}

onMounted(() => {
  fetchFleet()
})
</script>

<template>
  <div class="fleet-view pa-6">
    <div class="d-flex align-center justify-space-between mb-4">
      <div>
        <h1 class="text-h6 font-weight-black tracking-wider brand-title mb-1">
          РЕЕСТР ПОДВИЖНОГО СОСТАВА
        </h1>
        <p class="text-caption text-disabled font-mono">
          МОНИТОРИНГ И УЧЕТ ПАССАЖИРСКОГО ТРАНСПОРТА
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

    <v-card class="table-card" rounded="0">
      <v-data-table
        :headers="headers"
        :items="fleetData"
        :search="search"
        :loading="isLoading"
        density="compact"
        class="custom-table font-mono"
        no-data-text="Транспортные средства не обнаружены"
      >
        <!-- Слот ID -->
        <template #item.id="{ item, value }">
          <span class="text-grey-lighten-1 font-weight-bold">
            #{{ value ?? (item as any).id ?? (item as any).raw?.id }}
          </span>
        </template>

        <!-- Слот Госномера -->
        <template #item.numberPlate="{ item, value }">
          <span class="number-plate-badge">
            {{ value ?? (item as any).numberPlate ?? (item as any).raw?.numberPlate }}
          </span>
        </template>

        <!-- Слот Типа -->
        <template #item.type="{ item, value }">
          <span class="text-capitalize">
            {{ value ?? (item as any).type ?? (item as any).raw?.type }}
          </span>
        </template>

        <!-- Слот Вместимости -->
        <template #item.capacity="{ item, value }">
          <span>{{ value ?? (item as any).capacity ?? (item as any).raw?.capacity }} чел.</span>
        </template>

        <!-- Слот Статуса -->
        <template #item.status="{ item, value }">
          <v-chip
            size="x-small"
            label
            class="font-weight-bold"
            :style="{
              color: getStatusColor(value ?? (item as any).status ?? (item as any).raw?.status),
              borderColor: getStatusColor(value ?? (item as any).status ?? (item as any).raw?.status),
              backgroundColor: 'rgba(255, 255, 255, 0.02)'
            }"
            variant="outlined"
          >
            ● {{ getStatusText(value ?? (item as any).status ?? (item as any).raw?.status) }}
          </v-chip>
        </template>
      </v-data-table>
    </v-card>

    <v-alert v-if="errorMessage" type="error" variant="tonal" class="mt-4" rounded="0">
      <span class="font-mono text-caption">{{ errorMessage }}</span>
    </v-alert>
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