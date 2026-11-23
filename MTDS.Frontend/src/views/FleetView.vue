<script setup lang="ts">
import { ref, onMounted, computed, watch } from 'vue'
import apiClient from '../api'

// --- ИНТЕРФЕЙСЫ ---
interface TransportUnit {
  id: number
  numberPlate: string
  capacity: number
  status: string
  type: string
}

interface ShiftAssignment {
  id: number
  driverId: string
  driverName?: string
  driverPhone?: string
  transportId: number
  numberPlate?: string
  routeId: number
  pathNumber?: string
  shiftStart: string
  shiftEnd?: string | null
}

interface DriverItem {
  id: string | number
  name?: string
  surName?: string
  fullName: string
  email?: string
  phoneNumber?: string
  driverLicenseNumber?: string
}

interface RouteItem {
  id: number
  pathId: string
  transportType: string
}

// --- СОСТОЯНИЕ ВКЛАДОК И РОЛЕЙ ---
const activeTab = ref<'fleet' | 'shifts' | 'drivers'>('fleet')
const userRole = ref<string>(localStorage.getItem('role') || 'Dispatcher')
const isAdmin = computed(() => userRole.value === 'Admin')

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

// ==========================================
// ВКЛАДКА 1: ПОДВИЖНОЙ СОСТАВ (РЕЕСТР ТС)
// ==========================================
const fleetSearch = ref<string>('')
const isFleetLoading = ref<boolean>(false)
const fleetData = ref<TransportUnit[]>([])

const fleetHeaders = computed(() => [
  { title: 'ИД БОРТА', key: 'id', align: 'start' as const, width: '90px' },
  { title: 'ГОСНОМЕР', key: 'numberPlate', align: 'start' as const },
  { title: 'ТИП', key: 'type', align: 'start' as const },
  { title: 'ВМЕСТИМОСТЬ', key: 'capacity', align: 'center' as const },
  { title: 'СТАТУС ТЕЛЕМЕТРИИ', key: 'status', align: 'center' as const },
  { title: 'ДЕЙСТВИЯ', key: 'actions', align: 'center' as const, sortable: false, width: '180px' },
])

const getTransportTypeLabel = (type: string): string => {
  const t = (type || '').toLowerCase()
  if (t.includes('bus') && !t.includes('mini') && !t.includes('electro')) return 'Автобус'
  if (t.includes('tram') || t.includes('трам')) return 'Трамвай'
  if (t.includes('mini') || t.includes('марш')) return 'Маршрутка'
  if (t.includes('electro') || t.includes('электро')) return 'Электробус'
  return type || 'Автобус'
}

const statusDictionary: Record<string, { text: string; color: string }> = {
  active: { text: 'На маршруте', color: '#00E676' },
  on_route: { text: 'На маршруте', color: '#00E676' },
  in_garage: { text: 'В парке', color: '#8B949E' },
  garage: { text: 'В парке', color: '#8B949E' },
  broken: { text: 'В ремонте', color: '#FFD600' },
  repair: { text: 'В ремонте', color: '#FFD600' },
  emergency_stopped: { text: 'Аварийная остановка', color: '#FF1744' },
  stop: { text: 'Аварийная остановка', color: '#FF1744' },
  return_to_park: { text: 'Возврат в парк', color: '#FFD600' },
  returning_to_park: { text: 'Возврат в парк', color: '#FFD600' },
  radio_call_required: { text: 'Срочная радиосвязь', color: '#FF9100' },
  route_detour: { text: 'Объезд маршрута', color: '#29B6F6' },
  doors_unlock: { text: 'Разблокировка дверей', color: '#FF1744' },
  silent_alarm_ack: { text: 'Тихая тревога', color: '#FF1744' },
}

const getStatusInfo = (rawStatus: string): { text: string; color: string } => {
  const norm = (rawStatus || '').toLowerCase().replace(/[\s-]+/g, '_').trim()
  if (statusDictionary[norm]) return statusDictionary[norm]

  if (norm.includes('broken') || norm.includes('ремонт')) return { text: 'В ремонте', color: '#FFD600' }
  if (norm.includes('garage') || norm.includes('парк')) return { text: 'В парке', color: '#8B949E' }
  if (norm.includes('active') || norm.includes('маршрут') || norm.includes('лини')) return { text: 'На маршруте', color: '#00E676' }
  if (norm.includes('emergency') || norm.includes('авар') || norm === 'stop') return { text: 'Аварийнаяостановка', color: '#FF1744' }
  if (norm.includes('return')) return { text: 'Возврат в парк', color: '#FFD600' }

  const clean = (rawStatus || 'Неизвестно').replace(/_/g, ' ')
  return { text: clean.charAt(0).toUpperCase() + clean.slice(1), color: '#FFD600' }
}

const isCommandAllowed = (status: string): boolean => {
  const norm = (status || '').toLowerCase().replace(/[\s-]+/g, '_').trim()
  return !norm.includes('garage') && !norm.includes('парк') && !norm.includes('broken')
}

// Загрузка парка (GET /park)
const fetchFleet = async () => {
  try {
    isFleetLoading.value = true
    const response = await apiClient.get('/park')

    let rawList: any[] = []
    if (Array.isArray(response.data)) {
      rawList = response.data
    } else if (response.data && Array.isArray(response.data.park)) {
      rawList = response.data.park
    } else if (response.data && Array.isArray(response.data.Park)) {
      rawList = response.data.Park
    } else if (response.data && Array.isArray(response.data.items)) {
      rawList = response.data.items
    } else if (response.data && Array.isArray(response.data.data)) {
      rawList = response.data.data
    }

    fleetData.value = rawList.map((item: any) => ({
      id: Number(item.id ?? item.Id),
      numberPlate: String(item.numberPlate || item.NumberPlate || 'Б/Н'),
      capacity: Number(item.capacity ?? item.Capacity ?? 0),
      status: String(item.status || item.Status || 'in_garage'),
      type: String(item.type || item.Type || 'bus'),
    }))
  } catch (err: any) {
    console.error('[ОРБИТА-ПАРК] Ошибка загрузки парка:', err)
    showNotice(err.response?.data?.message || err.message || 'Сбой опроса реестра подвижного состава', true)
  } finally {
    isFleetLoading.value = false
  }
}

// Экспорт транспорта в CSV (UTF-8 BOM)
const exportFleetCsv = () => {
  if (!fleetData.value.length) {
    showNotice('Нет данных для экспорта', true)
    return
  }

  const csvHeaders = ['ID борта', 'Госномер', 'Тип', 'Вместимость', 'Статус']
  const csvRows = fleetData.value.map((item) => [
    item.id,
    `"${item.numberPlate}"`,
    `"${getTransportTypeLabel(item.type)}"`,
    item.capacity,
    `"${getStatusInfo(item.status).text}"`,
  ])

  const csvString = '\uFEFF' + [csvHeaders.join(';'), ...csvRows.map((r) => r.join(';'))].join('\r\n')
  const blob = new Blob([csvString], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.setAttribute('href', url)
  link.setAttribute('download', `orbita_fleet_${new Date().toISOString().slice(0, 10)}.csv`)
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
  URL.revokeObjectURL(url)

  showNotice(`Экспорт парка завершен: сохранено ${fleetData.value.length} записей`)
}

// --- БЫСТРАЯ СМЕНА СТАТУСА ТРАНСПОРТА (PATCH /park/{id}/status) ---
const isStatusDialogOpen = ref<boolean>(false)
const statusTargetVehicle = ref<TransportUnit | null>(null)
const selectedQuickStatus = ref<string>('active')
const isSavingStatus = ref<boolean>(false)

const quickStatusOptions = [
  { title: 'На маршруте', value: 'active' },
  { title: 'В парке', value: 'in_garage' },
  { title: 'В ремонте', value: 'broken' },
]

const openQuickStatusDialog = (unit: TransportUnit) => {
  statusTargetVehicle.value = unit
  selectedQuickStatus.value = ['active', 'in_garage', 'broken'].includes(unit.status) ? unit.status : 'active'
  isStatusDialogOpen.value = true
}

const handleSaveQuickStatus = async () => {
  if (!statusTargetVehicle.value) return
  try {
    isSavingStatus.value = true
    await apiClient.patch(`/park/${statusTargetVehicle.value.id}/status`, {
      status: selectedQuickStatus.value,
    })
    showNotice(`Статус борта ${statusTargetVehicle.value.numberPlate} изменен`)
    isStatusDialogOpen.value = false
    await fetchFleet()
  } catch (err: any) {
    console.error('[ОРБИТА-СТАТУС] Ошибка смены статуса:', err)
    showNotice(err.response?.data?.message || 'Не удалось обновить статус транспорта', true)
  } finally {
    isSavingStatus.value = false
  }
}

// --- СОЗДАНИЕ / РЕДАКТИРОВАНИЕ ТРАНСПОРТА (ADMIN ONLY) ---
const isVehicleDialogOpen = ref<boolean>(false)
const isEditingVehicle = ref<boolean>(false)
const isSavingVehicle = ref<boolean>(false)

const vehicleForm = ref({
  id: 0,
  numberPlate: '',
  type: 'bus',
  capacity: 85,
  status: 'in_garage',
})

const vehicleTypeOptions = [
  { title: 'Автобус', value: 'bus' },
  { title: 'Трамвай', value: 'tram' },
  { title: 'Маршрутка', value: 'minibus' },
  { title: 'Электробус', value: 'electrobus' },
]

// Строгая валидация номера ТС (Формат РФ)
const plateRegex = /^([АВЕКМНОРСТУХABEKMHOPCTYX]\d{3}[АВЕКМНОРСТУХABEKMHOPCTYX]{2}\d{2,3}|[АВЕКМНОРСТУХABEKMHOPCTYX]{2}\d{3}\d{2,3})$/i

const plateRules = [
  (v: string) => !!v?.trim() || 'Госномер обязателен',
  (v: string) => plateRegex.test((v || '').replace(/\s/g, '')) || 'Формат РФ (А123АА777 или АА12377)',
]

const capacityRules = [
  (v: number) => (v != null && v !== 0) || 'Укажите вместимость',
  (v: number) => (Number(v) >= 5 && Number(v) <= 300) || 'От 5 до 300 пассажиров',
]

const isVehicleSubmitAllowed = computed(() => {
  const p = vehicleForm.value.numberPlate.replace(/\s/g, '')
  const validPlate = plateRegex.test(p)
  const validCap = Number(vehicleForm.value.capacity) >= 5 && Number(vehicleForm.value.capacity) <= 300
  return Boolean(validPlate && validCap && !isSavingVehicle.value)
})

const openCreateVehicleDialog = () => {
  isEditingVehicle.value = false
  vehicleForm.value = {
    id: 0,
    numberPlate: '',
    type: 'bus',
    capacity: 85,
    status: 'in_garage',
  }
  isVehicleDialogOpen.value = true
}

const openEditVehicleDialog = (unit: TransportUnit) => {
  isEditingVehicle.value = true
  vehicleForm.value = {
    id: unit.id,
    numberPlate: unit.numberPlate,
    type: unit.type,
    capacity: unit.capacity,
    status: ['active', 'in_garage', 'broken'].includes(unit.status) ? unit.status : 'in_garage',
  }
  isVehicleDialogOpen.value = true
}

const handleSaveVehicle = async () => {
  if (!isVehicleSubmitAllowed.value) return

  try {
    isSavingVehicle.value = true
    const payload = {
      numberPlate: vehicleForm.value.numberPlate.trim().toUpperCase().replace(/\s/g, ''),
      type: vehicleForm.value.type,
      capacity: Number(vehicleForm.value.capacity),
      status: vehicleForm.value.status,
    }

    if (isEditingVehicle.value) {
      await apiClient.put(`/park/${vehicleForm.value.id}`, payload)
      showNotice(`Борт ${payload.numberPlate} успешно обновлен`)
    } else {
      await apiClient.post('/park', payload)
      showNotice(`Борт ${payload.numberPlate} добавлен в парк`)
    }

    isVehicleDialogOpen.value = false
    await fetchFleet()
  } catch (err: any) {
    console.error('[ОРБИТА-ПАРК] Ошибка сохранения ТС:', err)
    showNotice(err.response?.data?.message || 'Ошибка сохранения данных транспорта', true)
  } finally {
    isSavingVehicle.value = false
  }
}

// --- УДАЛЕНИЕ ТРАНСПОРТА (ADMIN ONLY) ---
const deleteVehicleConfirm = ref({
  open: false,
  id: 0,
  plate: '',
  loading: false,
})

const openDeleteVehicleDialog = (unit: TransportUnit) => {
  deleteVehicleConfirm.value = {
    open: true,
    id: unit.id,
    plate: unit.numberPlate,
    loading: false,
  }
}

const handleConfirmDeleteVehicle = async () => {
  try {
    deleteVehicleConfirm.value.loading = true
    await apiClient.delete(`/park/${deleteVehicleConfirm.value.id}`)
    showNotice(`Борт ${deleteVehicleConfirm.value.plate} списан и удален`)
    deleteVehicleConfirm.value.open = false
    await fetchFleet()
  } catch (err: any) {
    console.error('[ОРБИТА-ПАРК] Ошибка удаления ТС:', err)
    showNotice(err.response?.data?.message || 'Не удалось удалить транспортное средство', true)
  } finally {
    deleteVehicleConfirm.value.loading = false
  }
}

// --- ЖИВАЯ ТЕЛЕМЕТРИЯ ---
const isTelemetryDialogOpen = ref<boolean>(false)
const isTelemetryLoading = ref<boolean>(false)
const telemetryTarget = ref<TransportUnit | null>(null)
const telemetryData = ref<{
  lat: number
  lon: number
  speedKmh: number
  optimalSpeedKmh?: number | null
  scheduleStatus?: string | null
} | null>(null)

const openTelemetryDialog = async (unit: TransportUnit) => {
  telemetryTarget.value = unit
  isTelemetryDialogOpen.value = true
  await fetchTelemetryForVehicle(unit.id)
}

const fetchTelemetryForVehicle = async (transportId: number) => {
  try {
    isTelemetryLoading.value = true
    telemetryData.value = null

    const activeRes = await apiClient.get('/transport/active')
    if (activeRes.status === 204 || !activeRes.data) return

    const rawActive = Array.isArray(activeRes.data) ? activeRes.data : activeRes.data?.items || []
    const tracker = rawActive.find((t: any) => Number(t.transportId ?? t.TransportId ?? t.id) === transportId)

    if (!tracker || tracker.lat == null || tracker.lon == null) return

    const lat = Number(tracker.lat ?? tracker.Lat)
    const lon = Number(tracker.lon ?? tracker.Lon)
    const speedKmh = Number(tracker.speedKmh ?? tracker.SpeedKmh ?? tracker.speed ?? 0)

    let optimalSpeed: number | null = null
    let scheduleStatus: string | null = null

    try {
      const optimalRes = await apiClient.get(
        `/telemetry/optimal-speed?transportId=${transportId}&currentLat=${lat}&currentLon=${lon}`
      )
      if (optimalRes.data) {
        optimalSpeed = optimalRes.data.optimalSpeedKmh ?? optimalRes.data.OptimalSpeedKmh ?? null
        scheduleStatus = optimalRes.data.scheduleStatus ?? optimalRes.data.ScheduleStatus ?? optimalRes.data.status ?? null
      }
    } catch {
      // Игнорируем ошибку дополнительного расчета
    }

    telemetryData.value = {
      lat,
      lon,
      speedKmh,
      optimalSpeedKmh: optimalSpeed,
      scheduleStatus,
    }
  } catch (err: any) {
    console.error('[ОРБИТА-ТЕЛЕМЕТРИЯ] Ошибка:', err)
  } finally {
    isTelemetryLoading.value = false
  }
}

// --- МОДАЛКА ЭКСТРЕННОЙ КОМАНДЫ (БЕЗ ТЕНИ) ---
const isEmergencyDialogOpen = ref<boolean>(false)
const isSendingCommand = ref<boolean>(false)
const selectedTransport = ref<TransportUnit | null>(null)
const selectedCommand = ref<string>('STOP')
const commandReason = ref<string>('')

const commandOptions = [
  { title: 'Экстренная остановка (блокировка)', value: 'STOP' },
  { title: 'Возврат в парк (сход с линии)', value: 'RETURN_TO_PARK' },
  { title: 'Срочный вызов по рации', value: 'RADIO_CALL_REQUIRED' },
  { title: 'Объезд препятствия (смена маршрута)', value: 'ROUTE_DETOUR' },
  { title: 'Принудительная разблокировка дверей', value: 'DOORS_UNLOCK' },
  { title: 'Подтверждение тихой тревоги', value: 'SILENT_ALARM_ACK' },
]

const openEmergencyDialog = (unit: TransportUnit) => {
  if (!isCommandAllowed(unit.status)) return
  selectedTransport.value = { ...unit }
  selectedCommand.value = 'STOP'
  commandReason.value = ''
  isEmergencyDialogOpen.value = true
}

const closeEmergencyDialog = () => {
  isEmergencyDialogOpen.value = false
  selectedTransport.value = null
  commandReason.value = ''
}

const handleSendEmergencyCommand = async () => {
  if (!commandReason.value || commandReason.value.trim().length < 10 || !selectedTransport.value) return

  try {
    isSendingCommand.value = true
    const commandItem = commandOptions.find((o) => o.value === selectedCommand.value)
    const titleRu = commandItem ? commandItem.title : selectedCommand.value

    const payload = {
      transportId: Number(selectedTransport.value.id),
      actionType: selectedCommand.value,
      action: selectedCommand.value,
      command: selectedCommand.value,
      commandTitle: titleRu,
      actionTitle: titleRu,
      reason: commandReason.value.trim(),
      comment: commandReason.value.trim(),
    }

    const response = await apiClient.post('/emergency/command', payload)
    closeEmergencyDialog()

    const serverMsg = response.data?.message || response.data?.Message
    showNotice(serverMsg || `Приказ "${titleRu}" передан на борт ${selectedTransport.value.numberPlate}`)
    await fetchFleet()
  } catch (err: any) {
    console.error('[ОРБИТА-ЭКСТРЕННО] Ошибка:', err)
    showNotice(err.response?.data?.message || 'Ошибка отправки экстренной команды', true)
  } finally {
    isSendingCommand.value = false
  }
}

// ==========================================
// ВКЛАДКА 2: СМЕНЫ И МАРШРУТЫ (НАЗНАЧЕНИЕ)
// ==========================================
const shiftsList = ref<ShiftAssignment[]>([])
const isShiftsLoading = ref<boolean>(false)
const shiftFilter = ref<'active' | 'all'>('active')
const shiftsSearch = ref<string>('')

const shiftTableHeaders = [
  { title: 'ID СМЕНЫ', key: 'id', align: 'start' as const, width: '90px' },
  { title: 'МАРШРУТ', key: 'pathNumber', align: 'start' as const },
  { title: 'ТРАНСПОРТ', key: 'numberPlate', align: 'start' as const },
  { title: 'ВОДИТЕЛЬ', key: 'driverName', align: 'start' as const },
  { title: 'НАЧАЛО СМЕНЫ', key: 'shiftStart', align: 'start' as const },
  { title: 'СТАТУС СМЕНЫ', key: 'shiftEnd', align: 'center' as const },
  { title: 'УПРАВЛЕНИЕ', key: 'actions', align: 'center' as const, sortable: false },
]

const formatShiftTime = (iso?: string | null) => {
  if (!iso || iso.startsWith('0001-01-01')) return '—'
  const d = new Date(iso)
  if (isNaN(d.getTime())) return iso
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${pad(d.getDate())}.${pad(d.getMonth() + 1)} ${pad(d.getHours())}:${pad(d.getMinutes())}`
}

// Проверка активности смены: ShiftEnd null, 'null', пустой или 0001-01-01
const isShiftActive = (s: any): boolean => {
  const end = s.shiftEnd ?? s.ShiftEnd
  if (!end) return true
  if (end === 'null' || end === '') return true
  if (typeof end === 'string' && end.startsWith('0001-01-01')) return true
  return false
}

const fetchAssignments = async () => {
  try {
    isShiftsLoading.value = true
    const res = await apiClient.get('/assignments')

    // Распаковываем массив строго из ключа schedules (с фоллбеками)
    let raw: any[] = []
    if (Array.isArray(res.data)) {
      raw = res.data
    } else if (res.data && Array.isArray(res.data.schedules)) {
      raw = res.data.schedules
    } else if (res.data && Array.isArray(res.data.Schedules)) {
      raw = res.data.Schedules
    } else if (res.data && Array.isArray(res.data.items)) {
      raw = res.data.items
    } else if (res.data && Array.isArray(res.data.data)) {
      raw = res.data.data
    }

    shiftsList.value = raw.map((s: any) => {
      const endVal = s.shiftEnd ?? s.ShiftEnd
      const isClosed = endVal && endVal !== 'null' && !String(endVal).startsWith('0001-01-01')

      return {
        id: Number(s.id ?? s.Id),
        driverId: String(s.driverId ?? s.DriverId ?? ''),
        driverName: s.driverName || s.DriverName || 'Водитель СДМТ',
        driverPhone: s.driverPhone || s.DriverPhone || '',
        transportId: Number(s.transportId ?? s.TransportId ?? 0),
        numberPlate: s.numberPlate || s.NumberPlate || '—',
        routeId: Number(s.routeId ?? s.RouteId ?? 0),
        pathNumber: s.pathNumber || s.PathNumber || '—',
        shiftStart: s.shiftStart || s.ShiftStart || '',
        shiftEnd: isClosed ? endVal : null,
      }
    })
  } catch (err: any) {
    console.error('[ОРБИТА-СМЕНЫ] Ошибка загрузки смен:', err)
    showNotice(err.response?.data?.message || 'Не удалось загрузить смены', true)
  } finally {
    isShiftsLoading.value = false
  }
}

const displayedShifts = computed(() => {
  let list = shiftsList.value
  if (shiftFilter.value === 'active') {
    list = list.filter((s) => s.shiftEnd === null)
  }
  if (shiftsSearch.value.trim()) {
    const q = shiftsSearch.value.toLowerCase().trim()
    list = list.filter(
      (s) =>
        s.numberPlate?.toLowerCase().includes(q) ||
        s.pathNumber?.toLowerCase().includes(q) ||
        s.driverName?.toLowerCase().includes(q) ||
        String(s.id).includes(q)
    )
  }
  return list
})

// --- МОДАЛЬНОЕ ОКНО НАЗНАЧЕНИЯ НА МАРШРУТ ---
const isAssignDialogOpen = ref<boolean>(false)
const isAssigning = ref<boolean>(false)
const routesActiveList = ref<RouteItem[]>([])

const assignForm = ref({
  driverId: null as string | null,
  transportId: null as number | null,
  routeId: null as number | null,
})

// 1. ТОЛЬКО СВОБОДНЫЕ ВОДИТЕЛИ (не на активной смене)
const availableDriversForAssignment = computed(() => {
  const activeDriverIds = new Set(
    shiftsList.value.filter((s) => s.shiftEnd === null).map((s) => String(s.driverId).toLowerCase().trim())
  )
  return driversList.value.filter((d) => !activeDriverIds.has(String(d.id).toLowerCase().trim()))
})

// 2. ТОЛЬКО СВОБОДНЫЙ ТРАНСПОРТ (в депо и не на линии)
const availableTransportForAssignment = computed(() => {
  const activeTransportIds = new Set(
    shiftsList.value.filter((s) => s.shiftEnd === null).map((s) => s.transportId)
  )
  return fleetData.value.filter(
    (t) => t.status === 'in_garage' && !activeTransportIds.has(t.id)
  )
})

// 3. МАРШРУТЫ СТРОГО ПОД ТИП ВЫБРАННОГО ТРАНСПОРТА
const availableRoutesForAssignment = computed(() => {
  if (!assignForm.value.transportId) return []
  const selVehicle = fleetData.value.find((t) => t.id === assignForm.value.transportId)
  if (!selVehicle) return []

  return routesActiveList.value.filter(
    (r) => (r.transportType || '').toLowerCase() === (selVehicle.type || '').toLowerCase()
  )
})

// Автоматический сброс несовместимого маршрута при смене ТС
watch(
  () => assignForm.value.transportId,
  (newId) => {
    if (!newId) {
      assignForm.value.routeId = null
      return
    }
    const currentRoute = routesActiveList.value.find((r) => r.id === assignForm.value.routeId)
    const selVehicle = fleetData.value.find((t) => t.id === newId)
    if (currentRoute && selVehicle && currentRoute.transportType?.toLowerCase() !== selVehicle.type?.toLowerCase()) {
      assignForm.value.routeId = null
    }
  }
)

const isAssignSubmitAllowed = computed(() => {
  return Boolean(assignForm.value.driverId && assignForm.value.transportId && assignForm.value.routeId && !isAssigning.value)
})

const openAssignDialog = async () => {
  assignForm.value = { driverId: null, transportId: null, routeId: null }
  isAssignDialogOpen.value = true

  try {
    const routesRes = await apiClient.get('/routes/active').catch(() => ({ data: [] }))
    const rawRoutes = Array.isArray(routesRes.data) ? routesRes.data : routesRes.data?.items || []
    routesActiveList.value = rawRoutes.map((r: any) => ({
      id: Number(r.id ?? r.Id),
      pathId: String(r.pathId || r.PathId || r.pathNumber || r.PathNumber || ''),
      transportType: String(r.transportType || r.TransportType || 'bus'),
    }))

    await Promise.all([fetchDrivers(), fetchFleet(), fetchAssignments()])
  } catch (e) {
    console.error('Ошибка подготовки данных для назначения:', e)
  }
}

const handleAssignSubmit = async () => {
  if (!isAssignSubmitAllowed.value) return

  try {
    isAssigning.value = true
    await apiClient.post('/assignments', {
      driverId: assignForm.value.driverId,
      transportId: Number(assignForm.value.transportId),
      routeId: Number(assignForm.value.routeId),
    })

    showNotice('Смена открыта: борт успешно назначен на маршрут!')
    isAssignDialogOpen.value = false
    await Promise.all([fetchAssignments(), fetchFleet()])
  } catch (err: any) {
    console.error('[ОРБИТА-СМЕНЫ] Ошибка назначения:', err)
    showNotice(err.response?.data?.message || 'Сбой назначения борта на маршрут', true)
  } finally {
    isAssigning.value = false
  }
}

const handleEndShift = async (assignmentId: number) => {
  try {
    await apiClient.post(`/assignments/${assignmentId}/end`)
    showNotice('Смена закрыта. Борт возвращен в парк.')
    await Promise.all([fetchAssignments(), fetchFleet()])
  } catch (err: any) {
    console.error('[ОРБИТА-СМЕНЫ] Ошибка закрытия смены:', err)
    showNotice(err.response?.data?.message || 'Не удалось закрыть смену', true)
  }
}

// ==========================================
// ВКЛАДКА 3: ВОДИТЕЛИ (DRIVERS)
// ==========================================
const driversList = ref<DriverItem[]>([])
const isDriversLoading = ref<boolean>(false)
const driversSearch = ref<string>('')

const driverHeaders = computed(() => {
  const base = [
    { title: 'ФИО ВОДИТЕЛЯ', key: 'fullName', align: 'start' as const },
    { title: 'ТЕЛЕФОН', key: 'phoneNumber', align: 'start' as const },
    { title: 'ВОДИТЕЛЬСКОЕ УДОСТОВЕРЕНИЕ', key: 'driverLicenseNumber', align: 'start' as const },
    { title: 'EMAIL', key: 'email', align: 'start' as const },
  ]
  if (isAdmin.value) {
    base.push({ title: 'ДЕЙСТВИЯ', key: 'actions', align: 'start' as const})
  }
  return base
})

const fetchDrivers = async () => {
  try {
    isDriversLoading.value = true
    const res = await apiClient.get('/drivers')
    const raw = Array.isArray(res.data) ? res.data : res.data?.items || res.data?.data || []

    driversList.value = raw.map((d: any) => ({
      id: d.id ?? d.Id,
      name: d.name || d.Name || '',
      surName: d.surName || d.SurName || '',
      fullName: d.fullName || d.FullName || `${d.surName || ''} ${d.name || ''}`.trim() || 'Водитель',
      email: d.email || d.Email || '—',
      phoneNumber: d.phoneNumber || d.PhoneNumber || '—',
      driverLicenseNumber: d.driverLicenseNumber || d.DriverLicenseNumber || '—',
    }))
  } catch (err: any) {
    console.error('[ОРБИТА-ВОДИТЕЛИ] Ошибка загрузки:', err)
    showNotice(err.response?.data?.message || 'Не удалось загрузить реестр водителей', true)
  } finally {
    isDriversLoading.value = false
  }
}

// Экспорт водителей в CSV
const exportDriversCsv = () => {
  if (!driversList.value.length) {
    showNotice('Нет данных для экспорта', true)
    return
  }

  const csvHeaders = ['ID', 'ФИО', 'Телефон', 'Номер В/У', 'Email']
  const csvRows = driversList.value.map((d) => [
    d.id,
    `"${d.fullName}"`,
    `"${d.phoneNumber}"`,
    `"${d.driverLicenseNumber}"`,
    `"${d.email}"`,
  ])

  const csvString = '\uFEFF' + [csvHeaders.join(';'), ...csvRows.map((r) => r.join(';'))].join('\r\n')
  const blob = new Blob([csvString], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.setAttribute('href', url)
  link.setAttribute('download', `orbita_drivers_${new Date().toISOString().slice(0, 10)}.csv`)
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
  URL.revokeObjectURL(url)

  showNotice(`Экспорт водителей завершен: сохранено ${driversList.value.length} записей`)
}

// Модальное окно водителя
const isDriverDialogOpen = ref<boolean>(false)
const isEditingDriver = ref<boolean>(false)
const isSavingDriver = ref<boolean>(false)
const showDriverPassword = ref<boolean>(false)

const driverForm = ref({
  id: '' as string | number,
  name: '',
  surName: '',
  email: '',
  phoneNumber: '',
  driverLicenseNumber: '',
  password: '',
})

// Автоформат телефона: +7 (XXX) XXX-XX-XX
const onDriverPhoneInput = (e: Event) => {
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

  driverForm.value.phoneNumber = formatted
}

// Форматирование В/У с пробелами: XX XX XXXXXX (10 знаков)
const onLicenseInput =(e: Event) => {
  const input = e.target as HTMLInputElement
  let raw = input.value.toUpperCase().replace(/[^A-ZА-Я0-9]/gi, '').slice(0, 10)

  let formatted = raw
  if (raw.length > 2 && raw.length <= 4) {
    formatted = raw.slice(0, 2) + ' ' + raw.slice(2)
  } else if (raw.length > 4) {
    formatted = raw.slice(0, 2) + ' ' + raw.slice(2, 4) + ' ' + raw.slice(4)
  }

  driverForm.value.driverLicenseNumber = formatted
}

// Правила валидации для водителя
const driverNameRules = [
  (v: string) => !!v?.trim() || 'Имя обязательно',
  (v: string) => (v?.trim().length >= 2 && v?.trim().length <= 50) || 'От 2 до 50 символов',
  (v: string) => /^[а-яА-ЯёЁa-zA-Z\s-]+$/.test(v?.trim()) || 'Только буквы',
]

const driverSurNameRules = [
  (v: string) => !!v?.trim() || 'Фамилия обязательна',
  (v: string) => (v?.trim().length >= 2 && v?.trim().length <= 50) || 'От 2 до 50 символов',
  (v: string) => /^[а-яА-ЯёЁa-zA-Z\s-]+$/.test(v?.trim()) || 'Только буквы',
]

const driverEmailRules = [
  (v: string) => !!v?.trim() || 'Email обязателен',
  (v: string) => /^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$/.test(v?.trim()) || 'Формат: driver@mtds.ru',
]

const driverPhoneRules = [
  (v: string) => !!v?.trim() || 'Телефон обязателен',
  (v: string) => /^\+7 \(\d{3}\) \d{3}-\d{2}-\d{2}$/.test(v?.trim()) || 'Формат: +7 (XXX) XXX-XX-XX',
]

const driverLicenseRules = [
  (v: string) => !!v?.trim() || 'Номер В/У обязателен',
  (v: string) => (v?.trim().replace(/\s/g, '').length === 10) || 'Формат: XX XX XXXXXX (10 знаков)',
]

const driverPasswordRules = [
  (v: string) => (isEditingDriver.value || !!v) || 'Пароль обязателен при регистрации',
  (v: string) => (!v || v.length >= 6) || 'Минимум 6 символов',
]

// Кнопка сохранения: при редактировании пустой пароль разрешен
const isDriverSubmitAllowed = computed(() => {
  const f = driverForm.value
  const validName = f.name?.trim().length >= 2 && /^[а-яА-ЯёЁa-zA-Z\s-]+$/.test(f.name.trim())
  const validSurName = f.surName?.trim().length >= 2 && /^[а-яА-ЯёЁa-zA-Z\s-]+$/.test(f.surName.trim())
  const validEmail = /^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$/.test(f.email?.trim() || '')
  const validPhone = /^\+7 \(\d{3}\) \d{3}-\d{2}-\d{2}$/.test(f.phoneNumber?.trim() || '')
  const validLicense = (f.driverLicenseNumber || '').replace(/\s/g, '').length === 10

  const validPass = isEditingDriver.value
    ? (!f.password || f.password.length >= 6)
    : (!!f.password && f.password.length >= 6)

  return Boolean(validName && validSurName && validEmail && validPhone && validLicense && validPass && !isSavingDriver.value)
})

const openCreateDriverDialog = () => {
  isEditingDriver.value = false
  driverForm.value = {
    id: '',
    name: '',
    surName: '',
    email: '',
    phoneNumber: '',
    driverLicenseNumber: '',
    password: '',
  }
  showDriverPassword.value = false
  isDriverDialogOpen.value = true
}

const openEditDriverDialog = (driver: DriverItem) => {
  isEditingDriver.value = true

  // Применяем маску к уже существующему В/У
  let rawLic = (driver.driverLicenseNumber !== '—' ? driver.driverLicenseNumber || '' : '').replace(/[^A-ZА-Я0-9]/gi, '')
  let formattedLic = rawLic
  if (rawLic.length > 4) formattedLic = `${rawLic.slice(0, 2)} ${rawLic.slice(2, 4)} ${rawLic.slice(4, 10)}`

  driverForm.value = {
    id: driver.id,
    name: driver.name || '',
    surName: driver.surName || '',
    email: driver.email !== '—' ? driver.email || '' : '',
    phoneNumber: driver.phoneNumber !== '—' ? driver.phoneNumber || '' : '',
    driverLicenseNumber: formattedLic,
    password: '',
  }
  showDriverPassword.value = false
  isDriverDialogOpen.value = true
}

const handleSaveDriver = async () => {
  if (!isDriverSubmitAllowed.value) return

  try {
    isSavingDriver.value = true
    const payload: any = {
      name: driverForm.value.name.trim(),
      surName: driverForm.value.surName.trim(),
      email: driverForm.value.email.trim(),
      phoneNumber: driverForm.value.phoneNumber.trim(),
      driverLicenseNumber: driverForm.value.driverLicenseNumber.replace(/\s/g, '').trim().toUpperCase(),
    }

    if (driverForm.value.password.trim()) {
      payload.password = driverForm.value.password.trim()
    }

    if (isEditingDriver.value) {
      await apiClient.put(`/drivers/${driverForm.value.id}`, payload)
      showNotice(`Водитель ${payload.surName} ${payload.name} обновлен`)
    } else {
      await apiClient.post('/drivers', payload)
      showNotice(`Водитель ${payload.surName} ${payload.name} успешно добавлен`)
    }

    isDriverDialogOpen.value = false
    await fetchDrivers()
  } catch (err: any) {
    console.error('[ОРБИТА-ВОДИТЕЛИ] Ошибка сохранения:', err)
    showNotice(err.response?.data?.message || 'Ошибка сохранения данных водителя', true)
  } finally {
    isSavingDriver.value = false
  }
}

// Удаление водителя
const deleteDriverConfirm = ref({
  open: false,
  id: '' as string | number,
  name: '',
  loading: false,
})

const openDeleteDriverDialog = (driver: DriverItem) => {
  deleteDriverConfirm.value = {
    open: true,
    id: driver.id,
    name: driver.fullName,
    loading: false,
  }
}

const handleConfirmDeleteDriver = async () => {
  try {
    deleteDriverConfirm.value.loading = true
    await apiClient.delete(`/drivers/${deleteDriverConfirm.value.id}`)
    showNotice(`Водитель "${deleteDriverConfirm.value.name}" удален`)
    deleteDriverConfirm.value.open = false
    await fetchDrivers()
  } catch (err: any) {
    console.error('[ОРБИТА-ВОДИТЕЛИ] Ошибка удаления:', err)
    showNotice(err.response?.data?.message || 'Не удалось удалить водителя', true)
  } finally {
    deleteDriverConfirm.value.loading = false
  }
}

onMounted(() => {
  fetchFleet()
  fetchAssignments()
  fetchDrivers()
})
</script>

<template>
  <div class="fleet-view pa-6">
    <!-- Шапка страницы -->
    <div class="mb-4">
      <h1 class="text-h6 font-weight-black tracking-wider brand-title mb-1">
        ДИСПЕТЧЕРСКИЙ ПУЛЬТ АВТОПАРКА
      </h1>
      <p class="text-caption text-disabled font-mono mb-0">
        МОНИТОРИНГ ТЕХНИКИ, СМЕННЫХ ГРАФИКОВ И УЧЕТ ВОДИТЕЛЬСКОГО СОСТАВА
      </p>
    </div>

    <!-- Вкладки раздела -->
    <v-tabs v-model="activeTab" density="compact" color="#00E676" class="border-tabs mb-6 font-mono">
      <v-tab value="fleet" class="text-caption font-weight-bold">
        <v-icon icon="mdi-bus-multiple" size="18" class="mr-2" />
        ПОДВИЖНОЙ СОСТАВ ({{ fleetData.length }})
      </v-tab>
      <v-tab value="shifts" class="text-caption font-weight-bold">
        <v-icon icon="mdi-clipboard-account-outline" size="18" class="mr-2" />
        СМЕНЫ И МАРШРУТЫ ({{ shiftsList.filter((s) => s.shiftEnd === null).length }} на линии)
      </v-tab>
      <v-tab value="drivers" class="text-caption font-weight-bold">
        <v-icon icon="mdi-card-account-details-outline" size="18" class="mr-2" />
        ВОДИТЕЛИ ({{ driversList.length }})
      </v-tab>
    </v-tabs>

    <!-- ВКЛАДКА 1: ПОДВИЖНОЙ СОСТАВ -->
    <div v-show="activeTab === 'fleet'">
      <div class="d-flex align-center justify-space-between mb-3">
        <!-- Поиск + Кнопка обновить вплотную справа -->
        <div class="d-flex align-center" style="max-width: 440px; width: 100%;">
          <v-text-field
            v-model="fleetSearch"
            prepend-inner-icon="mdi-magnify"
            placeholder="Поиск по номеру, ID..."
            density="compact"
            variant="outlined"
            color="#00E676"
            hide-details
            class="font-mono search-input mr-2"
            autocomplete="off"
          />
          <v-tooltip text="Обновить парк" location="top">
            <template #activator="{ props }">
              <v-btn
                v-bind="props"
                icon="mdi-refresh"
                variant="outlined"
                size="small"
                rounded="0"
                :loading="isFleetLoading"
                class="border-subtle"
                @click="fetchFleet"
              />
            </template>
          </v-tooltip>
        </div>

        <div class="d-flex align-center">
          <v-btn
            variant="outlined"
            color="#00E676"
            size="small"
            rounded="0"
            prepend-icon="mdi-file-delimited-outline"
            class="font-mono mr-3 border-subtle"
            @click="exportFleetCsv"
          >
            ЭКСПОРТ В CSV
          </v-btn>

          <v-btn
            v-if="isAdmin"
            color="#00E676"
            variant="flat"
            rounded="0"
            size="small"
            prepend-icon="mdi-plus"
            class="font-mono font-weight-bold text-black"
            @click="openCreateVehicleDialog"
          >
            ДОБАВИТЬ ТРАНСПОРТ
          </v-btn>
        </div>
      </div>

      <v-card class="table-card" rounded="0">
        <v-data-table
          :headers="fleetHeaders"
          :items="fleetData"
          :search="fleetSearch"
          :loading="isFleetLoading"
          density="compact"
          class="custom-table font-mono"
          items-per-page-text="Строк на странице:"
          no-data-text="Транспортные средства не обнаружены"
        >
          <template #item.id="{ value }">
            <span class="text-grey-lighten-1 font-weight-bold">#{{ value }}</span>
          </template>

          <template #item.numberPlate="{ value }">
            <span class="number-plate-badge">{{ value }}</span>
          </template>

          <template #item.type="{ value }">
            <span>{{ getTransportTypeLabel(value) }}</span>
          </template>

          <template #item.capacity="{ value }">
            <span>{{ value }} чел.</span>
          </template>

          <!-- Кликабельный чип статуса -->
          <template #item.status="{ item, value }">
            <v-tooltip text="Нажмите для смены статуса" location="top">
              <template #activator="{ props }">
                <v-chip
                  v-bind="props"
                  size="x-small"
                  label
                  class="font-weight-bold status-chip cursor-pointer"
                  :style="{
                    color: getStatusInfo(value).color,
                    borderColor: getStatusInfo(value).color,
                    backgroundColor: 'rgba(255, 255, 255, 0.03)'
                  }"
                  variant="outlined"
                  @click="openQuickStatusDialog(item as any)"
                >
                  ● {{ getStatusInfo(value).text }}
                </v-chip>
              </template>
            </v-tooltip>
          </template>

          <!-- Раздел действий -->
          <template #item.actions="{ item }">
            <div class="d-flex align-center justify-center action-buttons-group">
              <!-- Телеметрия -->
              <v-tooltip text="GPS-телеметрия" location="top">
                <template #activator="{ props }">
                  <v-btn
                    v-bind="props"
                    icon="mdi-speedometer"
                    size="x-small"
                    variant="tonal"
                    color="#29B6F6"
                    rounded="0"
                    class="action-mini-btn"
                    @click="openTelemetryDialog(item as any)"
                  />
                </template>
              </v-tooltip>

              <!-- Боевая кнопка экстренного приказа (без квадратной тени) -->
              <v-tooltip
                :text="isCommandAllowed((item as any).status) ? 'Подача экстренной команды' : 'Борт не на маршруте (команды заблокированы)'"
                location="top"
              >
                <template #activator="{ props }">
                  <span v-bind="props" class="d-inline-block">
                    <v-btn
                      size="x-small"
                      variant="flat"
                      rounded="0"
                      :disabled="!isCommandAllowed((item as any).status)"
                      :class="[
                        'action-command-pill',
                        isCommandAllowed((item as any).status) ? 'action-command-active' : 'action-command-disabled'
                      ]"
                      @click="openEmergencyDialog(item as any)"
                    >
                      <v-icon icon="mdi-radio-handheld" size="13" class="mr-1" />
                      <span>ПРИКАЗ</span>
                    </v-btn>
                  </span>
                </template>
              </v-tooltip>

              <!-- Действия администратора -->
              <template v-if="isAdmin">
                <v-tooltip text="Редактировать" location="top">
                  <template #activator="{ props }">
                    <v-btn
                      v-bind="props"
                      icon="mdi-pencil"
                      size="x-small"
                      variant="text"
                      color="#FFD600"
                      class="action-mini-btn ml-1"
                      @click="openEditVehicleDialog(item as any)"
                    />
                  </template>
                </v-tooltip>

                <v-tooltip text="Списать / Удалить" location="top">
                  <template #activator="{ props }">
                    <v-btn
                      v-bind="props"
                      icon="mdi-delete"
                      size="x-small"
                      variant="text"
                      color="#FF1744"
                      class="action-mini-btn ml-1"
                      @click="openDeleteVehicleDialog(item as any)"
                    />
                  </template>
                </v-tooltip>
              </template>
            </div>
          </template>
        </v-data-table>
      </v-card>
    </div>

    <!-- ВКЛАДКА 2: СМЕНЫ И МАРШРУТЫ -->
    <div v-show="activeTab === 'shifts'">
      <div class="d-flex align-center justify-space-between mb-3">
        <!-- Поиск + Кнопка обновить вплотную справа -->
        <div class="d-flex align-center" style="max-width: 440px; width: 100%;">
          <v-text-field
            v-model="shiftsSearch"
            prepend-inner-icon="mdi-magnify"
            placeholder="Поиск по номеру, водителю, маршруту..."
            density="compact"
            variant="outlined"
            color="#00E676"
            hide-details
            class="font-mono search-input mr-2"
            autocomplete="off"
          />
          <v-tooltip text="Обновить смены" location="top">
            <template #activator="{ props }">
              <v-btn
                v-bind="props"
                icon="mdi-refresh"
                variant="outlined"
                size="small"
                rounded="0"
                :loading="isShiftsLoading"
                class="border-subtle"
                @click="() => { fetchAssignments(); fetchFleet(); }"
              />
            </template>
          </v-tooltip>
        </div>

        <div class="d-flex align-center">
          <v-btn-toggle v-model="shiftFilter" mandatory density="compact" rounded="0" class="border-tabs font-mono mr-3">
            <v-btn value="active" size="small" class="text-caption">АКТИВНЫЕ СМЕНЫ</v-btn>
            <v-btn value="all" size="small" class="text-caption">ВСЯ ИСТОРИЯ</v-btn>
          </v-btn-toggle>

          <v-btn
            color="#00E676"
            variant="flat"
            rounded="0"
            size="small"
            prepend-icon="mdi-account-plus-outline"
            class="font-mono font-weight-bold text-black"
            @click="openAssignDialog"
          >
            НАЗНАЧИТЬ НА МАРШРУТ
          </v-btn>
        </div>
      </div>

      <v-card class="table-card" rounded="0">
        <v-data-table
          :headers="shiftTableHeaders"
          :items="displayedShifts"
          :loading="isShiftsLoading"
          density="compact"
          class="custom-table font-mono"
          items-per-page-text="Строк на странице:"
          no-data-text="Записи о сменных графиках отсутствуют"
        >
          <template #item.id="{ value }">
            <span class="text-disabled">#{{ value }}</span>
          </template>

          <template #item.pathNumber="{ value }">
            <v-chip size="x-small" label color="#29B6F6" variant="outlined" class="font-weight-bold">
              {{ value }}
            </v-chip>
          </template>

          <template #item.numberPlate="{ value }">
            <span class="number-plate-badge">{{ value }}</span>
          </template>

          <template #item.driverName="{ item, value }">
            <div class="d-flex flex-column">
              <span class="font-weight-bold text-white">{{ value }}</span>
              <span v-if="(item as any).driverPhone" class="text-disabled" style="font-size: 10px;">
                {{ (item as any).driverPhone }}
              </span>
            </div>
          </template>

          <template #item.shiftStart="{ value }">
            <span class="text-grey-lighten-2">{{ formatShiftTime(value) }}</span>
          </template>

          <template #item.shiftEnd="{ value }">
            <v-chip v-if="!value" size="x-small" label color="#00E676" variant="tonal" class="font-weight-bold">
              ● В РЕЙСЕ
            </v-chip>
            <span v-else class="text-disabled">{{ formatShiftTime(value) }}</span>
          </template>

          <template #item.actions="{ item }">
            <v-btn
              v-if="!(item as any).shiftEnd"
              size="x-small"
              variant="outlined"
              color="#FF1744"
              rounded="0"
              class="font-mono"
              @click="handleEndShift((item as any).id)"
            >
              ЗАВЕРШИТЬ СМЕНУ
            </v-btn>
            <span v-else class="text-disabled font-mono" style="font-size: 11px;">ЗАВЕРШЕНА</span>
          </template>
        </v-data-table>
      </v-card>
    </div>

    <!-- ВКЛАДКА 3: ВОДИТЕЛИ -->
    <div v-show="activeTab === 'drivers'">
      <div class="d-flex align-center justify-space-between mb-3">
        <!-- Поиск + Кнопка обновить вплотную справа -->
        <div class="d-flex align-center" style="max-width: 440px; width: 100%;">
          <v-text-field
            v-model="driversSearch"
            prepend-inner-icon="mdi-magnify"
            placeholder="Поиск по ФИО, В/У, телефону..."
            density="compact"
            variant="outlined"
            color="#00E676"
            hide-details
            class="font-mono search-input mr-2"
            autocomplete="off"
          />
          <v-tooltip text="Обновить водителей" location="top">
            <template #activator="{ props }">
              <v-btn
                v-bind="props"
                icon="mdi-refresh"
                variant="outlined"
                size="small"
                rounded="0"
                :loading="isDriversLoading"
                class="border-subtle"
                @click="fetchDrivers"
              />
            </template>
          </v-tooltip>
        </div>

        <div class="d-flex align-center">
          <v-btn
            variant="outlined"
            color="#00E676"
            size="small"
            rounded="0"
            prepend-icon="mdi-file-delimited-outline"
            class="font-mono mr-3 border-subtle"
            @click="exportDriversCsv"
          >
            ЭКСПОРТ В CSV
          </v-btn>

          <v-btn
            v-if="isAdmin"
            color="#00E676"
            variant="flat"
            rounded="0"
            size="small"
            prepend-icon="mdi-account-plus"
            class="font-mono font-weight-bold text-black"
            @click="openCreateDriverDialog"
          >
            ДОБАВИТЬ ВОДИТЕЛЯ
          </v-btn>
        </div>
      </div>

      <v-card class="table-card" rounded="0">
        <v-data-table
          :headers="driverHeaders"
          :items="driversList"
          :search="driversSearch"
          :loading="isDriversLoading"
          density="compact"
          class="custom-table font-mono"
          items-per-page-text="Строк на странице:"
          no-data-text="Водители не найдены"
        >
          <template #item.fullName="{ value }">
            <span class="font-weight-bold text-white">{{ value }}</span>
          </template>

          <template #item.phoneNumber="{ value }">
            <span class="text-grey-lighten-2">{{ value }}</span>
          </template>

          <template #item.driverLicenseNumber="{ value }">
            <span class="number-plate-badge">{{ value }}</span>
          </template>

          <template #item.email="{ value }">
            <span class="text-disabled">{{ value }}</span>
          </template>

          <template #item.actions="{ item }">
            <div class="d-flex align-center justify-center">
              <v-tooltip text="Редактировать" location="top">
                <template #activator="{ props }">
                  <v-btn
                    v-bind="props"
                    icon="mdi-pencil"
                    size="x-small"
                    variant="text"
                    color="#FFD600"
                    class="mr-1"
                    @click="openEditDriverDialog(item as any)"
                  />
                </template>
              </v-tooltip>

              <v-tooltip text="Удалить" location="top">
                <template #activator="{ props }">
                  <v-btn
                    v-bind="props"
                    icon="mdi-delete"
                    size="x-small"
                    variant="text"
                    color="#FF1744"
                    @click="openDeleteDriverDialog(item as any)"
                  />
                </template>
              </v-tooltip>
            </div>
          </template>
        </v-data-table>
      </v-card>
    </div>

    <!-- ДИАЛОГ 1: БЫСТРАЯ СМЕНА СТАТУСА ТРАНСПОРТА -->
    <v-dialog v-model="isStatusDialogOpen" max-width="420" persistent transition="fade-transition">
      <v-card class="admin-dialog-card pa-4" rounded="0">
        <div class="d-flex align-center justify-space-between mb-2">
          <div class="text-subtitle-2 font-mono font-weight-bold text-white">
            СМЕНА СТАТУСА • [{{ statusTargetVehicle?.numberPlate }}]
          </div>
          <v-btn icon="mdi-close" variant="text" size="x-small" @click="isStatusDialogOpen = false" />
        </div>
        <v-divider class="border-opacity-10 mb-4" />

        <div class="text-caption text-disabled font-mono mb-1">РАБОЧИЙ СТАТУС ТРАНСПОРТА</div>
        <v-select
          v-model="selectedQuickStatus"
          :items="quickStatusOptions"
          item-title="title"
          item-value="value"
          variant="outlined"
          density="compact"
          color="#00E676"
          rounded="0"
          class="font-mono mb-4 input-tactical"
          hide-details
        />

        <div class="d-flex justify-end">
          <v-btn variant="text" size="small" rounded="0" class="font-mono mr-2" @click="isStatusDialogOpen = false">
            ОТМЕНА
          </v-btn>
          <v-btn
            color="#00E676"
            variant="flat"
            size="small"
            rounded="0"
            class="font-mono font-weight-bold text-black px-4"
            :loading="isSavingStatus"
            @click="handleSaveQuickStatus"
          >
            СОХРАНИТЬ
          </v-btn>
        </div>
      </v-card>
    </v-dialog>

    <!-- ДИАЛОГ 2: СОЗДАНИЕ / РЕДАКТИРОВАНИЕ ТРАНСПОРТА (ВЫРАВНЕННЫЕ ПОЛЯ) -->
    <v-dialog v-model="isVehicleDialogOpen" max-width="500" persistent transition="fade-transition">
      <v-card class="admin-dialog-card pa-4" rounded="0">
        <div class="d-flex align-center justify-space-between mb-2">
          <div class="text-subtitle-2 font-mono font-weight-bold text-white">
            {{ isEditingVehicle ? `РЕДАКТИРОВАНИЕ БОРТА #${vehicleForm.id}` : 'ДОБАВЛЕНИЕ НОВОГО БОРТА' }}
          </div>
          <v-btn icon="mdi-close" variant="text" size="x-small" @click="isVehicleDialogOpen = false" />
        </div>
        <v-divider class="border-opacity-10 mb-4" />

        <v-form autocomplete="off" @submit.prevent="handleSaveVehicle">
          <div class="field-label-fixed text-caption text-disabled font-mono mb-1">ГОСНОМЕР ТРАНСПОРТА (ФОРМАТ РФ) *</div>
          <v-text-field
            v-model="vehicleForm.numberPlate"
            placeholder="А123АА777 или АА12377"
            variant="outlined"
            density="compact"
            color="#00E676"
            class="font-mono mb-2 input-tactical"
            autocomplete="off"
            hide-details="auto"
            :rules="plateRules"
            @input="(e: any) => vehicleForm.numberPlate = e.target.value.toUpperCase().replace(/\s/g, '')"
          />

          <!-- Выровненная строка: Тип и Вместимость -->
          <v-row class="mb-2 align-start">
            <v-col cols="6" class="py-1">
              <div class="field-label-fixed text-caption text-disabled font-mono mb-1">ТИП ТРАНСПОРТА</div>
              <v-select
                v-model="vehicleForm.type"
                :items="vehicleTypeOptions"
                item-title="title"
                item-value="value"
                variant="outlined"
                density="compact"
                color="#00E676"
                class="font-mono input-tactical"
                hide-details="auto"
              />
            </v-col>
            <v-col cols="6" class="py-1">
              <div class="field-label-fixed text-caption text-disabled font-mono mb-1">ВМЕСТИМОСТЬ (ЧЕЛ.) *</div>
              <v-text-field
                v-model.number="vehicleForm.capacity"
                type="number"
                placeholder="50"
                variant="outlined"
                density="compact"
                color="#00E676"
                class="font-mono input-tactical"
                hide-details="auto"
                :rules="capacityRules"
              />
            </v-col>
          </v-row>

          <div class="field-label-fixed text-caption text-disabled font-mono mb-1">РАБОЧИЙ СТАТУС</div>
          <v-select
            v-model="vehicleForm.status"
            :items="quickStatusOptions"
            item-title="title"
            item-value="value"
            variant="outlined"
            density="compact"
            color="#00E676"
            class="font-mono mb-4 input-tactical"
            hide-details="auto"
          />

          <div class="d-flex justify-end mt-4">
            <v-btn variant="text" size="small" rounded="0" class="font-mono mr-2" @click="isVehicleDialogOpen = false">
              ОТМЕНА
            </v-btn>
            <v-btn
              type="submit"
              color="#00E676"
              variant="flat"
              size="small"
              rounded="0"
              class="font-mono font-weight-bold text-black px-4"
              :loading="isSavingVehicle"
              :disabled="!isVehicleSubmitAllowed"
            >
              {{ isEditingVehicle ? 'СОХРАНИТЬ' : 'ДОБАВИТЬ' }}
            </v-btn>
          </div>
        </v-form>
      </v-card>
    </v-dialog>

    <!-- ДИАЛОГ 3: УДАЛЕНИЕ ТРАНСПОРТА -->
    <v-dialog v-model="deleteVehicleConfirm.open" max-width="440" persistent transition="fade-transition">
      <v-card class="admin-dialog-card pa-5" rounded="0">
        <div class="d-flex align-center justify-space-between mb-3">
          <div class="d-flex align-center">
            <v-icon icon="mdi-alert-octagon-outline" color="#FF1744" size="26" class="mr-2" />
            <div class="text-subtitle-2 font-weight-bold font-mono text-white">СПИСАНИЕ ТРАНСПОРТА</div>
          </div>
          <v-btn icon="mdi-close" variant="text" size="x-small" @click="deleteVehicleConfirm.open = false" />
        </div>
        <v-divider class="border-opacity-10 mb-4" />
        <div class="text-body-2 font-mono text-grey-lighten-2 mb-5">
          Вы уверены, что хотите списать и удалить борт
          <span class="text-red-accent-2 font-weight-bold">"{{ deleteVehicleConfirm.plate }}"</span>?
        </div>
        <div class="d-flex justify-end">
          <v-btn variant="text" size="small" rounded="0" class="font-mono mr-2" @click="deleteVehicleConfirm.open = false">
            ОТМЕНА
          </v-btn>
          <v-btn
            color="#FF1744"
            variant="flat"
            size="small"
            rounded="0"
            class="font-mono font-weight-bold text-white px-4"
            :loading="deleteVehicleConfirm.loading"
            @click="handleConfirmDeleteVehicle"
          >
            СПИСАТЬ
          </v-btn>
        </div>
      </v-card>
    </v-dialog>

    <!-- ДИАЛОГ 4: ЖИВАЯ ТЕЛЕМЕТРИЯ -->
    <v-dialog v-model="isTelemetryDialogOpen" max-width="460" transition="fade-transition">
      <v-card class="telemetry-dialog-card pa-5" rounded="0">
        <div class="d-flex align-center justify-space-between mb-2">
          <div class="d-flex align-center">
            <v-icon icon="mdi-speedometer" color="#29B6F6" size="22" class="mr-2" />
            <span class="text-subtitle-2 font-mono font-weight-bold text-white">
              ТЕЛЕМЕТРИЯ • [{{ telemetryTarget?.numberPlate }}]
            </span>
          </div>
          <v-btn icon="mdi-close" variant="text" size="x-small" @click="isTelemetryDialogOpen = false" />
        </div>
        <v-divider class="border-opacity-10 mb-4" />

        <div v-if="isTelemetryLoading" class="py-6 text-center">
          <v-progress-circular indeterminate color="#29B6F6" size="32" class="mb-2" />
          <div class="text-caption font-mono text-grey-lighten-1">ОПРОС GPS-ТРЕКЕРА...</div>
        </div>

        <div v-else-if="telemetryData" class="font-mono">
          <div class="telemetry-stat-box pa-3 mb-2">
            <div class="text-caption text-disabled">ТЕКУЩИЕ КООРДИНАТЫ</div>
            <div class="text-body-2 font-weight-bold text-cyan-accent-2">
              {{ telemetryData.lat.toFixed(5) }}, {{ telemetryData.lon.toFixed(5) }}
            </div>
          </div>

          <v-row class="mb-1">
            <v-col cols="6">
              <div class="telemetry-stat-box pa-3">
                <div class="text-caption text-disabled">ТЕКУЩАЯ СКОРОСТЬ</div>
                <div class="text-h6 font-weight-black text-white">
                  {{ telemetryData.speedKmh }} <span class="text-caption text-disabled">км/ч</span>
                </div>
              </div>
            </v-col>
            <v-col cols="6">
              <div class="telemetry-stat-box pa-3">
                <div class="text-caption text-disabled">РЕКОМЕНДОВАННАЯ</div>
                <div class="text-h6 font-weight-black text-green-accent-3">
                  {{ telemetryData.optimalSpeedKmh ?? '—' }} <span class="text-caption text-disabled">км/ч</span>
                </div>
              </div>
            </v-col>
          </v-row>

          <div class="telemetry-stat-box pa-3 mb-4">
            <div class="text-caption text-disabled">СТАТУС СОБЛЮДЕНИЯ ГРАФИКА</div>
            <div class="text-body-2 font-weight-bold text-grey-lighten-1">
              {{ telemetryData.scheduleStatus || 'В рамках нормативного интервала' }}
            </div>
          </div>

          <v-btn
            block
            variant="outlined"
            color="#29B6F6"
            rounded="0"
            size="small"
            prepend-icon="mdi-refresh"
            class="font-mono"
            @click="telemetryTarget && fetchTelemetryForVehicle(telemetryTarget.id)"
          >
            ОБНОВИТЬ ТЕЛЕМЕТРИЮ
          </v-btn>
        </div>

        <div v-else class="text-center py-6 font-mono text-disabled">
          <v-icon icon="mdi-signal-off" size="36" class="mb-2" />
          <div class="text-body-2">Нет активногосигнала GPS от трекера</div>
          <div class="text-caption mt-1">Борт не передает данные координат в реальном времени</div>
        </div>
      </v-card>
    </v-dialog>

    <!-- ДИАЛОГ 5: НАЗНАЧЕНИЕ НА МАРШРУТ (УМНАЯ ФИЛЬТРАЦИЯ + КРЕСТИК) -->
    <v-dialog v-model="isAssignDialogOpen" max-width="520" persistent transition="fade-transition">
      <v-card class="admin-dialog-card pa-4" rounded="0">
        <div class="d-flex align-center justify-space-between mb-2">
          <div class="text-subtitle-2 font-mono font-weight-bold text-white">
            ОТКРЫТИЕ СМЕНЫ: ВЫПУСК БОРТА НА ЛИНИЮ
          </div>
          <v-btn icon="mdi-close" variant="text" size="x-small" @click="isAssignDialogOpen = false" />
        </div>
        <v-divider class="border-opacity-10 mb-4" />

        <!-- 1. Свободные водители -->
        <div class="text-caption text-disabled font-mono mb-1">
          1. СВОБОДНЫЙ ВОДИТЕЛЬ (НЕ НА ЛИНИИ) *
        </div>
        <v-autocomplete
          v-model="assignForm.driverId"
          :items="availableDriversForAssignment"
          item-title="fullName"
          item-value="id"
          placeholder="Выберите водителя..."
          variant="outlined"
          density="compact"
          color="#00E676"
          class="font-mono mb-3 input-tactical"
          no-data-text="Все водители сейчас на рейсе"
          hide-details
        />

        <!-- 2. Свободный транспорт в парке -->
        <div class="text-caption text-disabled font-mono mb-1">
          2. СВОБОДНЫЙ БОРТ В ДЕПО *
        </div>
        <v-autocomplete
          v-model="assignForm.transportId"
          :items="availableTransportForAssignment"
          :item-title="(item) => `${item.numberPlate} (${getTransportTypeLabel(item.type)})`"
          item-value="id"
          placeholder="Выберите свободный транспорт..."
          variant="outlined"
          density="compact"
          color="#00E676"
          class="font-mono mb-3 input-tactical"
          no-data-text="Нет свободного транспорта в депо"
          hide-details
        />

        <!-- 3. Маршрут под выбранный тип транспорта -->
        <div class="text-caption text-disabled font-mono mb-1">
          3. МАРШРУТ (ФИЛЬТР ПО ТИПУ ТС) *
        </div>
        <v-autocomplete
          v-model="assignForm.routeId"
          :items="availableRoutesForAssignment"
          :item-title="(item) => `Маршрут № ${item.pathId.toUpperCase()} (${getTransportTypeLabel(item.transportType)})`"
          item-value="id"
          :disabled="!assignForm.transportId"
          :placeholder="assignForm.transportId ? 'Выберите совместимый маршрут...' : 'Сначала выберите свободный борт...'"
          variant="outlined"
          density="compact"
          color="#00E676"
          class="font-mono mb-4 input-tactical"
          no-data-text="Нет доступных маршрутов для выбранного типа ТС"
          hide-details
        />

        <div class="d-flex justify-end">
          <v-btn variant="text" size="small" rounded="0" class="font-mono mr-2" @click="isAssignDialogOpen = false">
            ОТМЕНА
          </v-btn>
          <v-btn
            color="#00E676"
            variant="flat"
            size="small"
            rounded="0"
            class="font-mono font-weight-bold text-black px-4"
            :loading="isAssigning"
            :disabled="!isAssignSubmitAllowed"
            @click="handleAssignSubmit"
          >
            ВЫПУСТИТЬ НА ЛИНИЮ
          </v-btn>
        </div>
      </v-card>
    </v-dialog>

    <!-- ДИАЛОГ 6: СОЗДАНИЕ / РЕДАКТИРОВАНИЕ ВОДИТЕЛЯ (ВЫРОВНЕННЫЕ ПОЛЯ) -->
    <v-dialog v-model="isDriverDialogOpen" max-width="520" persistent transition="fade-transition">
      <v-card class="admin-dialog-card pa-4" rounded="0">
        <div class="d-flex align-center justify-space-between mb-2">
          <div class="text-subtitle-2 font-mono font-weight-bold text-white">
            {{ isEditingDriver ? 'РЕДАКТИРОВАНИЕ ВОДИТЕЛЯ' : 'РЕГИСТРАЦИЯ ВОДИТЕЛЯ' }}
          </div>
          <v-btn icon="mdi-close" variant="text" size="x-small" @click="isDriverDialogOpen = false" />
        </div>
        <v-divider class="border-opacity-10 mb-4" />

        <v-form autocomplete="off" @submit.prevent="handleSaveDriver">
          <v-row class="mb-1 align-start">
            <v-col cols="6" class="py-1">
              <div class="field-label-fixed text-caption text-disabled font-mono mb-1">ИМЯ *</div>
              <v-text-field
                v-model="driverForm.name"
                placeholder="Иван"
                variant="outlined"
                density="compact"
                color="#00E676"
                class="font-mono input-tactical"
                autocomplete="off"
                hide-details="auto"
                :rules="driverNameRules"
              />
            </v-col>
            <v-col cols="6" class="py-1">
              <div class="field-label-fixed text-caption text-disabled font-mono mb-1">ФАМИЛИЯ *</div>
              <v-text-field
                v-model="driverForm.surName"
                placeholder="Иванов"
                variant="outlined"
                density="compact"
                color="#00E676"
                class="font-mono input-tactical"
                autocomplete="off"
                hide-details="auto"
                :rules="driverSurNameRules"
              />
            </v-col>
          </v-row>

          <div class="field-label-fixed text-caption text-disabled font-mono mb-1 mt-1">СЛУЖЕБНЫЙ EMAIL *</div>
          <v-text-field
            v-model="driverForm.email"
            placeholder="driver@mtds.ru"
            variant="outlined"
            density="compact"
            color="#00E676"
            class="font-mono mb-2 input-tactical"
            autocomplete="off"
            hide-details="auto"
            :rules="driverEmailRules"
          />

          <!-- Выровненная строка: Телефон и В/У -->
          <v-row class="mb-1 align-start">
            <v-col cols="6" class="py-1">
              <div class="field-label-fixed text-caption text-disabled font-mono mb-1">ТЕЛЕФОН СВЯЗИ *</div>
              <v-text-field
                :model-value="driverForm.phoneNumber"
                placeholder="+7 (999) 000-00-00"
                variant="outlined"
                density="compact"
                color="#00E676"
                class="font-mono input-tactical"
                autocomplete="off"
                hide-details="auto"
                :rules="driverPhoneRules"
                @input="onDriverPhoneInput"
              />
            </v-col>
            <v-col cols="6" class="py-1">
              <div class="field-label-fixed text-caption text-disabled font-mono mb-1">НОМЕР В/У (XX XX XXXXXX) *</div>
              <v-text-field
                :model-value="driverForm.driverLicenseNumber"
                placeholder="50 12 345678"
                variant="outlined"
                density="compact"
                color="#00E676"
                class="font-mono input-tactical"
                autocomplete="off"
                maxlength="12"
                hide-details="auto"
                :rules="driverLicenseRules"
                @input="onLicenseInput"
              />
            </v-col>
          </v-row>

          <div class="field-label-fixed text-caption text-disabled font-mono mb-1 mt-2">
            ПАРОЛЬ {{ isEditingDriver ? '(Оставьте пустым, чтобы не менять)' : '*' }}
          </div>
          <v-text-field
            v-model="driverForm.password"
            :type="showDriverPassword ? 'text' : 'password'"
            placeholder="••••••••••••"
            :append-inner-icon="showDriverPassword ? 'mdi-eye-off-outline' : 'mdi-eye-outline'"
            variant="outlined"
            density="compact"
            color="#00E676"
            class="font-mono mb-4 input-tactical"
            autocomplete="new-password"
            hide-details="auto"
            :rules="driverPasswordRules"
            @click:append-inner="showDriverPassword = !showDriverPassword"
          />

          <div class="d-flex justify-end mt-4">
            <v-btn variant="text" size="small" rounded="0" class="font-mono mr-2" @click="isDriverDialogOpen = false">
              ОТМЕНА
            </v-btn>
            <v-btn
              type="submit"
              color="#00E676"
              variant="flat"
              size="small"
              rounded="0"
              class="font-mono font-weight-bold text-black px-4"
              :loading="isSavingDriver"
              :disabled="!isDriverSubmitAllowed"
            >
              {{ isEditingDriver ? 'СОХРАНИТЬ' : 'ЗАРЕГИСТРИРОВАТЬ' }}
            </v-btn>
          </div>
        </v-form>
      </v-card>
    </v-dialog>

    <!-- ДИАЛОГ 7: УДАЛЕНИЕ ВОДИТЕЛЯ -->
    <v-dialog v-model="deleteDriverConfirm.open" max-width="440" persistent transition="fade-transition">
      <v-card class="admin-dialog-card pa-5" rounded="0">
        <div class="d-flex align-center justify-space-between mb-3">
          <div class="d-flex align-center">
            <v-icon icon="mdi-alert-octagon-outline" color="#FF1744" size="26" class="mr-2" />
            <div class="text-subtitle-2 font-weight-bold font-mono text-white">УДАЛЕНИЕ ВОДИТЕЛЯ</div>
          </div>
          <v-btn icon="mdi-close" variant="text" size="x-small" @click="deleteDriverConfirm.open = false" />
        </div>
        <v-divider class="border-opacity-10 mb-4" />
        <div class="text-body-2 font-mono text-grey-lighten-2 mb-5">
          Вы уверены, что хотите удалить водителя
          <span class="text-red-accent-2 font-weight-bold">"{{ deleteDriverConfirm.name }}"</span>?
        </div>
        <div class="d-flex justify-end">
          <v-btn variant="text" size="small" rounded="0" class="font-mono mr-2" @click="deleteDriverConfirm.open = false">
            ОТМЕНА
          </v-btn>
          <v-btn
            color="#FF1744"
            variant="flat"
            size="small"
            rounded="0"
            class="font-mono font-weight-bold text-white px-4"
            :loading="deleteDriverConfirm.loading"
            @click="handleConfirmDeleteDriver"
          >
            УДАЛИТЬ
          </v-btn>
        </div>
      </v-card>
    </v-dialog>

    <!-- ДИАЛОГ 8: ЭКСТРЕННАЯ КОМАНДА -->
    <v-dialog v-model="isEmergencyDialogOpen" max-width="520" persistent transition="fade-transition">
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
          <v-btn icon="mdi-close" variant="text" size="small" rounded="0" :disabled="isSendingCommand" @click="closeEmergencyDialog" />
        </div>
        <v-divider class="border-opacity-20" />
        <v-card-text class="pa-5">
          <div class="text-caption text-grey-lighten-1 font-mono mb-1">ТИП КОМАНДЫ ДЛЯ БОРТОВОГО КОМПЬЮТЕРА</div>
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
          <div class="text-caption text-grey-lighten-1 font-mono mb-1">ПРИЧИНА / ОБОСНОВАНИЕ ДЛЯ БОРТОВОГО ЖУРНАЛА *</div>
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
          />
        </v-card-text>
        <v-divider class="border-opacity-20" />
        <v-card-actions class="pa-4 d-flex justify-end">
          <v-btn variant="text" rounded="0" size="small" class="font-mono mr-2" :disabled="isSendingCommand" @click="closeEmergencyDialog">
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
.border-tabs {
  border-bottom: 1px solid #21262D;
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

/* Фиксированная высота меток полей для идеального выравнивания по горизонтали */
.field-label-fixed {
  min-height: 20px;
  line-height: 20px;
}

/* Действия в таблице */
.action-buttons-group {
  gap: 2px;
}
.action-mini-btn {
  width: 26px !important;
  height: 26px !important;
}

/* Кнопка приказа: тень полностью убрана */
.action-command-pill {
  height: 24px !important;
  padding: 0 8px !important;
  letter-spacing: 0.06em;
  font-size: 10px !important;
  font-weight: bold;
}
.action-command-active {
  background-color: rgba(255, 23, 68, 0.15) !important;
  border: 1px solid #FF1744 !important;
  color: #FF1744 !important;
  transition: all 0.2s ease;
}
.action-command-active:hover {
  background-color: #FF1744 !important;
  color: #FFFFFF !important;
  box-shadow: none !important; /* УБРАНА ТЕНЬ */
}
.action-command-disabled {
  background-color: rgba(255, 255, 255, 0.03) !important;
  border: 1px solid #30363D !important;
  color: #6E7681 !important;
  opacity: 0.5;
}

/* Карточки модальных окон */
.admin-dialog-card,
.telemetry-dialog-card {
  background-color: #161B22 !important;
  border: 1px solid #30363D !important;
}
.telemetry-stat-box {
  background-color: #0E1116;
  border: 1px solid #21262D;
}
.emergency-dialog-card {
  background-color: #161B22 !important;
  border: 1px solid #FF1744 !important;
  box-shadow: 0 0 30px rgba(255, 23, 68, 0.2) !important;
}
.emergency-header {
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