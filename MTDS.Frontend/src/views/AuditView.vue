<script setup lang="ts">
import { ref, onMounted } from 'vue'
import apiClient from '../api'

interface AuditRecord {
  id: string | number
  dispatcher: string
  transport: string
  action: string
  createdAt: string
}

const search = ref<string>('')
const isLoading = ref<boolean>(false)
const auditLogs = ref<AuditRecord[]>([])
const errorMessage = ref<string | null>(null)

const headers = [
  { title: 'ВРЕМЯ СОБЫТИЯ', key: 'createdAt', align: 'start' as const },
  { title: 'ОПЕРАТОР / ДИСПЕТЧЕР', key: 'dispatcher', align: 'start' as const },
  { title: 'ТРАНСПОРТ', key: 'transport', align: 'start' as const },
  { title: 'ДЕЙСТВИЕ / ИНЦИДЕНТ', key: 'action', align: 'start' as const },
  { title: 'ID', key: 'id', align: 'end' as const },
]

const formatDateTime = (dateStr: string) => {
  if (!dateStr) return '—'
  const date = new Date(dateStr)
  if (isNaN(date.getTime())) return dateStr

  const pad = (n: number) => String(n).padStart(2, '0')
  const day = pad(date.getDate())
  const month = pad(date.getMonth() + 1)
  const year = date.getFullYear()
  const hours = pad(date.getHours())
  const minutes = pad(date.getMinutes())
  const seconds = pad(date.getSeconds())

  return `${day}.${month}.${year} ${hours}:${minutes}:${seconds}`
}

const fetchAuditLogs = async () => {
  try {
    isLoading.value = true
    errorMessage.value = null

    const response = await apiClient.get('/audit')
    console.log('[ОРБИТА-АУДИТ] Ответ сервера /audit:', response.data)

  // Извлекаем массив с учетом поля Logs / logs
    let rawList: any[] = []
    if (Array.isArray(response.data)) {
      rawList = response.data
    } else if (response.data && Array.isArray(response.data.logs)) {
      rawList = response.data.logs // <-- вот ключ C#
    } else if (response.data && Array.isArray(response.data.Logs)) {
      rawList = response.data.Logs
    } else if (response.data && Array.isArray(response.data.data)) {
      rawList = response.data.data
    }

    // Нормализация полей под контракт
    auditLogs.value = rawList.map((item: any) => ({
      id: item.id ?? item.Id ?? '—',
      dispatcher:
        item.dispatcherEmail ||
        item.DispatcherEmail ||
        (item.dispatcherId || item.DispatcherId ? `Диспетчер #${(item.dispatcherId || item.DispatcherId).substring(0, 8)}` : 'Диспетчер'),
      transport:
        item.transportPlate ||
        item.TransportPlate ||
        (item.transportId || item.TransportId ? `Борт #${item.transportId || item.TransportId}` : '—'),
      action: item.action || item.Action || 'Действие не указано',
      createdAt: item.createdAt || item.CreatedAt || '',
    }))

    console.log('[ОРБИТА-АУДИТ] Нормализовано записей журнала:', auditLogs.value.length)
  } catch (err: any) {
    console.error('[ОРБИТА-АУДИТ] Ошибка журнала аудита:', err)
    errorMessage.value = err.message || 'Ошибка загрузки протокола системных событий'
  } finally {
    isLoading.value = false
  }
}

onMounted(() => {
  fetchAuditLogs()
})
</script>

<template>
  <div class="audit-view pa-6">
    <div class="d-flex align-center justify-space-between mb-4">
      <div>
        <h1 class="text-h6 font-weight-black tracking-wider brand-title mb-1">
          ЖУРНАЛ АУДИТА СИСТЕМЫ
        </h1>
        <p class="text-caption text-disabled font-mono">
          ПРОТОКОЛИРОВАНИЕ ДЕЙСТВИЙ ОПЕРАТОРОВ И ИНЦИДЕНТОВ ТЕЛЕМЕТРИИ
        </p>
      </div>

      <div class="d-flex align-center">
        <v-text-field
          v-model="search"
          prepend-inner-icon="mdi-magnify"
          placeholder="Фильтр по журналу..."
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
          @click="fetchAuditLogs"
        />
      </div>
    </div>

    <v-card class="table-card" rounded="0">
      <v-data-table
        :headers="headers"
        :items="auditLogs"
        :search="search"
        :loading="isLoading"
        density="compact"
        class="custom-table font-mono"
        no-data-text="Записи в журнале аудита отсутствуют"
      >
        <template #item.createdAt="{ item, value }">
          <span class="text-grey-lighten-2 font-weight-medium">
            {{ formatDateTime(value ?? (item as any).createdAt ?? (item as any).raw?.createdAt) }}
          </span>
        </template>

        <template #item.dispatcher="{ item, value }">
          <span class="text-grey-lighten-1">
            {{ value ?? (item as any).dispatcher ?? (item as any).raw?.dispatcher }}
          </span>
        </template>

        <template #item.transport="{ item, value }">
          <span class="number-plate-badge">
            {{ value ?? (item as any).transport ?? (item as any).raw?.transport }}
          </span>
        </template>

        <template #item.action="{ item, value }">
          <span class="action-cell">
            {{ value ?? (item as any).action ?? (item as any).raw?.action }}
          </span>
        </template>

        <template #item.id="{ item, value }">
          <span class="text-disabled">
            #{{ value ?? (item as any).id ?? (item as any).raw?.id }}
          </span>
        </template>
      </v-data-table>
    </v-card>

    <v-alert v-if="errorMessage" type="error" variant="tonal" class="mt-4" rounded="0">
      <span class="font-mono text-caption">{{ errorMessage }}</span>
    </v-alert>
  </div>
</template>

<style scoped>
.audit-view {
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
  padding: 2px 6px;
  border: 1px solid #30363D;
  border-radius: 2px;
  color: #00E676;
  font-size: 11px;
}
.action-cell {
  color: #E6EDF3;
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
  font-size: 12px !important;
}
.font-mono {
  font-family: 'JetBrains Mono', Consolas, monospace;
}
</style>