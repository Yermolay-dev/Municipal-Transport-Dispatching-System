<script setup lang="ts">
import { ref, onMounted } from 'vue'
import apiClient from '../api'

interface AuditRecord {
  id: string | number
  dispatcher: string
  rawDispatcherId: string
  transport: string
  commandName: string
  commandBadgeColor: string
  reason: string
  createdAt: string
}

const search = ref<string>('')
const isLoading = ref<boolean>(false)
const auditLogs = ref<AuditRecord[]>([])
const errorMessage = ref<string | null>(null)

// Колонки таблицы с четким разделением Команды и Причины
const headers = [
  { title: 'ВРЕМЯ СОБЫТИЯ', key: 'createdAt', align: 'start' as const },
  { title: 'ОПЕРАТОР / ДИСПЕТЧЕР', key: 'dispatcher', align: 'start' as const },
  { title: 'ТРАНСПОРТ', key: 'transport', align: 'start' as const },
  { title: 'КОМАНДА / СОБЫТИЕ', key: 'commandName', align: 'start' as const },
  { title: 'ПРИЧИНА / ДЕТАЛИ', key: 'reason', align: 'start' as const },
  { title: 'ID', key: 'id', align: 'end' as const },
]

// Словарь расшифровки кодов команд в человекочитаемые русские названия
const commandCodeDictionary: Record<string, { title: string; color: string }> = {
  STOP: { title: 'Экстренная остановка', color: '#FF1744' },
  RETURN_TO_PARK: { title: 'Возврат в парк', color: '#FFD600' },
  RADIO_CALL_REQUIRED: { title: 'Срочный вызов по рации', color: '#FF9100' },
  ROUTE_DETOUR: { title: 'Объезд препятствия', color: '#29B6F6' },
  DOORS_UNLOCK: { title: 'Разблокировка дверей', color: '#FF1744' },
  SILENT_ALARM_ACK: { title: 'Подтверждение тихой тревоги', color: '#FF1744' },
}

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

// Разбор записи аудита на правильную команду и причину
const parseActionField = (item: any): { commandName: string; commandColor: string; reasonText: string } => {
  const rawAction = String(item.action || item.Action || '').trim()
  const explicitCommand = String(item.command || item.Command || item.actionType || item.ActionType || '').trim()
  const explicitReason = String(item.reason || item.Reason || item.comment || item.Comment || '').trim()

  // 1. Проверяем, есть ли явный код команды (например STOP или RETURN_TO_PARK)
  const codeCandidate = explicitCommand || (commandCodeDictionary[rawAction] ? rawAction : '')

  if (commandCodeDictionary[codeCandidate]) {
    return {
      commandName: commandCodeDictionary[codeCandidate].title,
      commandColor: commandCodeDictionary[codeCandidate].color,
      reasonText: explicitReason || (rawAction !== codeCandidate ? rawAction : 'Обоснование не указано'),
    }
  }

  // 2. Если в Action записана строка вида "Команда: X, Причина: Y" или "X: Y"
  if (rawAction.includes(':')) {
    const parts = rawAction.split(':')
    const firstPart = parts[0].trim().replace(/^команда\s*/i, '')
    const secondPart = parts.slice(1).join(':').trim().replace(/^причина\s*/i, '')

    // Если первая часть была кодом команды
    if (commandCodeDictionary[firstPart]) {
      return {
        commandName: commandCodeDictionary[firstPart].title,
        commandColor: commandCodeDictionary[firstPart].color,
        reasonText: secondPart || 'Обоснование не указано',
      }
    }

    return {
      commandName: firstPart || 'Диспетчерский приказ',
      commandColor: '#29B6F6',
      reasonText: secondPart || '—',
    }
  }

  // 3. Штатные системные логи из базы данных
  return {
    commandName: rawAction || 'Системное событие',
    commandColor: rawAction.includes('ЭКСТРЕН') ? '#FF1744' : '#00E676',
    reasonText: explicitReason || 'Штатная фиксация телеметрии',
  }
}

const fetchAuditLogs = async () => {
  try {
    isLoading.value = true
    errorMessage.value = null

    const response = await apiClient.get('/audit')

    let rawList: any[] = []
    if (Array.isArray(response.data)) {
      rawList = response.data
    } else if (response.data && Array.isArray(response.data.logs)) {
      rawList = response.data.logs
    } else if (response.data && Array.isArray(response.data.Logs)) {
      rawList = response.data.Logs
    } else if (response.data && Array.isArray(response.data.data)) {
      rawList = response.data.data
    }

    auditLogs.value = rawList.map((item: any) => {
      const rawId = item.dispatcherId || item.DispatcherId || ''
      const email = item.dispatcherEmail || item.DispatcherEmail

      let displayDispatcher = 'Диспетчер'
      if (email) {
        displayDispatcher = email
      } else if (rawId) {
        displayDispatcher = `#${rawId.substring(0, 8).toUpperCase()}`
      }

      const { commandName, commandColor, reasonText } = parseActionField(item)

      return {
        id: item.id ?? item.Id ?? '—',
        dispatcher: displayDispatcher,
        rawDispatcherId: rawId,
        transport:
          item.transportPlate ||
          item.TransportPlate ||
          (item.transportId || item.TransportId ? `Борт #${item.transportId || item.TransportId}` : '—'),
        commandName,
        commandBadgeColor: commandColor,
        reason: reasonText,
        createdAt: item.createdAt || item.CreatedAt || '',
      }
    })
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
        items-per-page-text="Строк на странице:"
        no-data-text="Записи в журнале аудита отсутствуют"
      >
        <!-- Время -->
        <template #item.createdAt="{ item, value }">
          <span class="text-grey-lighten-2 font-weight-medium">
            {{ formatDateTime(value ?? (item as any).createdAt ?? (item as any).raw?.createdAt) }}
          </span>
        </template>

        <!-- Диспетчер -->
        <template #item.dispatcher="{ item, value }">
          <v-tooltip :text="(item as any).rawDispatcherId ? `Идентификатор: ${(item as any).rawDispatcherId}` : 'Оператор СДМТ'" location="top">
            <template #activator="{ props }">
              <span v-bind="props" class="dispatcher-badge font-weight-bold">
                {{ value ?? (item as any).dispatcher ?? (item as any).raw?.dispatcher }}
              </span>
            </template>
          </v-tooltip>
        </template><!-- Транспорт -->
        <template #item.transport="{ item, value }">
          <span class="number-plate-badge">
            {{ value ?? (item as any).transport ?? (item as any).raw?.transport }}
          </span>
        </template>

        <!-- Колонка КОМАНДА: всегда русское официальное наименование -->
        <template #item.commandName="{ item, value }">
          <v-chip
            size="x-small"
            label
            class="font-weight-bold"
            :style="{
              color: (item as any).commandBadgeColor,
              borderColor: (item as any).commandBadgeColor,
              backgroundColor: 'rgba(255, 255, 255, 0.02)'
            }"
            variant="outlined"
          >
            {{ value ?? (item as any).commandName ?? (item as any).raw?.commandName }}
          </v-chip>
        </template>

        <!-- Колонка ПРИЧИНА: текст обоснования диспетчера -->
        <template #item.reason="{ item, value }">
          <span class="reason-cell text-grey-lighten-2">
            {{ value ?? (item as any).reason ?? (item as any).raw?.reason }}
          </span>
        </template>

        <!-- ID -->
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
.dispatcher-badge {
  background-color: rgba(0, 230, 118, 0.08);
  border: 1px solid rgba(0, 230, 118, 0.3);
  padding: 1px 6px;
  border-radius: 2px;
  color: #00E676;
  font-size: 11px;
}
.number-plate-badge {
  background-color: #21262D;
  padding: 2px 6px;
  border: 1px solid #30363D;
  border-radius: 2px;
  color: #E6EDF3;
  font-size: 11px;
}
.reason-cell {
  font-size: 12px;
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