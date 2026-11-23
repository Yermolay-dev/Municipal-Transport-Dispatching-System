<script setup lang="ts">
import { ref, onMounted, onUnmounted, nextTick, watch } from 'vue'
import apiClient from '../api'

declare const ymaps: any

// --- ИНТЕРФЕЙСЫ ---
interface StopItem {
  id: number
  name: string
  lat: number
  lon: number
}

interface RouteSummaryItem {
  id: number
  pathId: string
  transportType: string
}

interface RouteStopDraft {
  stopId: number
  stopName: string
  stopOrder: number
  minutesFromStart: number
}

// --- СОСТОЯНИЕ ВКЛАДОК ---
const activeTab = ref<'stops' | 'routes'>('stops')

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

// --- УПРАВЛЕНИЕ СЛАЙДЕРАМИ-РАЗДЕЛИТЕЛЯМИ (SPLITTER) ---
const stopsSplitContainer = ref<HTMLDivElement | null>(null)
const stopsSplitPercent = ref<number>(56)
const isDraggingStopsSplit = ref<boolean>(false)

const routesSplitContainer = ref<HTMLDivElement | null>(null)
const routesSplitPercent = ref<number>(44)
const isDraggingRoutesSplit = ref<boolean>(false)

// Логика перетаскивания для вкладки Остановок
const startStopsSplitDrag = () => {
  isDraggingStopsSplit.value = true
  document.addEventListener('mousemove', onStopsSplitDrag)
  document.addEventListener('mouseup', stopStopsSplitDrag)
}

const onStopsSplitDrag = (e: MouseEvent) => {
  if (!isDraggingStopsSplit.value || !stopsSplitContainer.value) return
  const rect = stopsSplitContainer.value.getBoundingClientRect()
  const offset = e.clientX - rect.left
  const percent = (offset / rect.width) * 100
  if (percent >= 30 && percent <= 70) {
    stopsSplitPercent.value = percent
    if (stopsMapInstance) stopsMapInstance.container.fitToViewport()
  }
}

const stopStopsSplitDrag = () => {
  isDraggingStopsSplit.value = false
  document.removeEventListener('mousemove', onStopsSplitDrag)
  document.removeEventListener('mouseup', stopStopsSplitDrag)
  if (stopsMapInstance) stopsMapInstance.container.fitToViewport()
}

// Логика перетаскивания для вкладки Маршрутов
const startRoutesSplitDrag = () => {
  isDraggingRoutesSplit.value = true
  document.addEventListener('mousemove', onRoutesSplitDrag)
  document.addEventListener('mouseup', stopRoutesSplitDrag)
}

const onRoutesSplitDrag = (e: MouseEvent) => {
  if (!isDraggingRoutesSplit.value || !routesSplitContainer.value) return
  const rect = routesSplitContainer.value.getBoundingClientRect()
  const offset = e.clientX - rect.left
  const percent = (offset / rect.width) * 100
  if (percent >= 30 && percent <= 70) {
    routesSplitPercent.value = percent
    if (routesMapInstance) routesMapInstance.container.fitToViewport()
  }
}

const stopRoutesSplitDrag = () => {
  isDraggingRoutesSplit.value = false
  document.removeEventListener('mousemove', onRoutesSplitDrag)
  document.removeEventListener('mouseup', stopRoutesSplitDrag)
  if (routesMapInstance) routesMapInstance.container.fitToViewport()
}

// --- ДИАЛОГ ПОДТВЕРЖДЕНИЯ УДАЛЕНИЯ ---
const deleteConfirmDialog = ref({
  open: false,
  type: 'stop' as 'stop' | 'route',
  id: 0,
  title: '',
  loading: false,
})

const openDeleteConfirm = (type: 'stop' | 'route', id: number, title: string) => {
  deleteConfirmDialog.value = {
    open: true,
    type,
    id,
    title,
    loading: false,
  }
}

// ==============================================================
// ВКЛАДКА 1: УПРАВЛЕНИЕ ОСТАНОВКАМИ
// ==============================================================
const stopsList = ref<StopItem[]>([])
const isStopsLoading = ref<boolean>(false)
const stopsSearch = ref<string>('')

const editingStopId = ref<number | null>(null)

const stopForm = ref({
  name: '',
  lat: null as number | null,
  lon: null as number | null,
})
const isSavingStop = ref<boolean>(false)

const stopsMapContainer = ref<HTMLDivElement | null>(null)
let stopsMapInstance: any = null
let tempStopPlacemark: any = null

const stopTableHeaders = [
  { title: 'ID', key: 'id', align: 'start' as const, width: '65px' },
  { title: 'НАЗВАНИЕ ОСТАНОВКИ', key: 'name', align: 'start' as const },
  { title: 'ШИРОТА (LAT)', key: 'lat', align: 'start' as const },
  { title: 'ДОЛГОТА (LON)', key: 'lon', align: 'start' as const },
  { title: 'ДЕЙСТВИЯ', key: 'actions', align: 'center' as const, sortable: false, width: '100px' },
]

const fetchStops = async () => {
  try {
    isStopsLoading.value = true
    const response = await apiClient.get('/stops')

    let raw: any[] = []
    if (Array.isArray(response.data)) raw = response.data
    else if (response.data && Array.isArray(response.data.stops)) raw = response.data.stops
    else if (response.data && Array.isArray(response.data.Stops)) raw = response.data.Stops

    stopsList.value = raw.map((s: any) => ({
      id: Number(s.id ?? s.Id),
      name: s.name || s.Name || 'Без названия',
      lat: Number(s.lat ?? s.Lat),
      lon: Number(s.lon ?? s.Lon),
    }))
  } catch (err: any) {
    console.error('Ошибка загрузки остановок:', err)
    showNotice(err.response?.data?.message || 'Не удалось загрузить реестр остановок', true)
  } finally {
    isStopsLoading.value = false
  }
}

const initStopsMap = () => {
  if (typeof ymaps === 'undefined' || !stopsMapContainer.value || stopsMapInstance) return

  ymaps.ready(() => {
    stopsMapInstance = new ymaps.Map(
      stopsMapContainer.value,
      {
        center: [55.4311, 37.5458],
        zoom: 13,
        controls: ['zoomControl'],
      },
      { suppressMapOpenBlock: true }
    )

    stopsMapInstance.events.add('click', (e: any) => {
      const coords = e.get('coords')
      stopForm.value.lat = Number(coords[0].toFixed(6))
      stopForm.value.lon = Number(coords[1].toFixed(6))
      updateStopMarker(coords)
    })
  })
}

const updateStopMarker = (coords: [number, number]) => {
  if (!stopsMapInstance) return
  if (tempStopPlacemark) {
    tempStopPlacemark.geometry.setCoordinates(coords)
  } else {
    tempStopPlacemark = new ymaps.Placemark(
      coords,
      { hintContent: 'Позиция остановки' },
      { preset: 'islands#redDotIcon' }
    )
    stopsMapInstance.geoObjects.add(tempStopPlacemark)
  }
}

const startEditStop = (stop: StopItem) => {
  editingStopId.value = stop.id
  stopForm.value = {
    name: stop.name,
    lat: stop.lat,
    lon: stop.lon,
  }

  if (stopsMapInstance && stop.lat && stop.lon) {
    const coords: [number, number] = [stop.lat, stop.lon]
    stopsMapInstance.setCenter(coords, 14, { duration: 300 })
    updateStopMarker(coords)
  }
}

const cancelEditStop = () => {
  editingStopId.value = null
  stopForm.value = { name: '', lat: null, lon: null }
  if (tempStopPlacemark && stopsMapInstance) {
    stopsMapInstance.geoObjects.remove(tempStopPlacemark)
    tempStopPlacemark = null
  }
}

const handleSaveStop = async () => {
  if (!stopForm.value.name.trim() || !stopForm.value.lat || !stopForm.value.lon) {
    showNotice('Заполните название и укажите точку на карте', true)
    return
  }

  try {
    isSavingStop.value = true
    const payload = {
      name: stopForm.value.name.trim(),
      lat: stopForm.value.lat,
      lon: stopForm.value.lon,
    }

    if (editingStopId.value) {
      await apiClient.put(`/stops/${editingStopId.value}`, payload)
      showNotice(`Остановка "${payload.name}" успешно обновлена!`)
    } else {
      await apiClient.post('/stops', payload)
      showNotice(`Остановка "${payload.name}" успешно добавлена!`)
    }

    cancelEditStop()
    await fetchStops()
  } catch (err: any) {
    console.error('Ошибка сохранения остановки:', err)
    showNotice(err.response?.data?.message || err.message || 'Сбой сохранения остановки', true)
  } finally {
    isSavingStop.value = false
  }
}

// ==============================================================
// ВКЛАДКА 2: МАРШРУТЫ И КОНСТРУКТОР
// ==============================================================
const routesList = ref<RouteSummaryItem[]>([])
const isRoutesLoading = ref<boolean>(false)
const routesSearch = ref<string>('')

const editingRouteId = ref<number | null>(null)

const routeForm = ref({
  pathNumber: '',
  transportType: 'bus',
})

const transportTypeOptions = [
  { title: 'Автобус', value: 'bus' },
  { title: 'Троллейбус', value: 'trolleybus' },
  { title: 'Трамвай', value: 'tram' },
  { title: 'Маршрутка', value: 'minibus' },
]

const selectedStopIdToAdd = ref<number | null>(null)
const minutesFromStartToAdd = ref<number>(0)
const routeStopsList = ref<RouteStopDraft[]>([])

const routesMapContainer = ref<HTMLDivElement | null>(null)
let routesMapInstance: any = null
let routePolyline: any = null
const routePoints = ref<[number, number][]>([])
const isSavingRoute = ref<boolean>(false)

const routeTableHeaders = [
  { title: 'ID', key: 'id', align: 'start' as const, width: '55px' },
  { title: 'НОМЕР', key: 'pathId', align: 'start' as const },
  { title: 'ТИП ТРАНСПОРТА', key: 'transportType', align: 'start' as const },
  { title: 'ДЕЙСТВИЯ', key: 'actions', align: 'center' as const, sortable: false, width: '90px' },
]

const fetchRoutes = async () => {
  try {
    isRoutesLoading.value = true
    let res: any = null
    const candidateUrls = ['/routes/active', '/routes']

    for (const url of candidateUrls) {
      try {
        res = await apiClient.get(url)
        if (res && (res.status === 200 || Array.isArray(res.data) || res.data?.items)) {
          break
        }
      } catch (e: any) {
        if (e.response?.status === 404) continue
        throw e
      }
    }

    if (!res) {
      routesList.value = []
      return
    }

    const raw = Array.isArray(res.data) ? res.data : (res.data?.items || res.data?.data || [])

    routesList.value = raw.map((r: any) => ({
      id: Number(r.id ?? r.Id),
      pathId: String(r.pathId || r.PathId || r.pathNumber || r.PathNumber || ''),
      transportType: String(r.transportType || r.TransportType || 'bus'),
    }))
  } catch (err: any) {
    console.error('Ошибка загрузки списка маршрутов:', err)
  } finally {
    isRoutesLoading.value = false
  }
}

const initRoutesMap = () => {
  if (typeof ymaps === 'undefined' || !routesMapContainer.value || routesMapInstance) return

  ymaps.ready(() => {
    routesMapInstance = new ymaps.Map(
      routesMapContainer.value,
      {
        center: [55.4311, 37.5458],
        zoom: 13,
        controls: ['zoomControl'],
      },
      { suppressMapOpenBlock: true }
    )

    routesMapInstance.events.add('click', (e: any) => {
      const coords = e.get('coords')
      const fixedPoint: [number, number] = [Number(coords[0].toFixed(6)), Number(coords[1].toFixed(6))]
      routePoints.value.push(fixedPoint)
      updatePolylineOnMap()
    })
  })
}

const updatePolylineOnMap = () => {
  if (!routesMapInstance) return

  if (routePolyline) {
    routesMapInstance.geoObjects.remove(routePolyline)
    routePolyline = null
  }

  if (routePoints.value.length > 0) {
    routePolyline = new ymaps.Polyline(
      routePoints.value,
      {},
      {
        strokeColor: '#00B0FF',
        strokeWidth: 5,
        strokeOpacity: 0.9,
      }
    )
    routesMapInstance.geoObjects.add(routePolyline)
  }
}

const removeLastRoutePoint = () => {
  if (routePoints.value.length > 0) {
    routePoints.value.pop()
    updatePolylineOnMap()
  }
}

const clearRoutePoints = () => {
  routePoints.value = []
  updatePolylineOnMap()
}

const addStopToRoute = () => {
  if (!selectedStopIdToAdd.value) return

  const stopObj = stopsList.value.find((s) => s.id === selectedStopIdToAdd.value)
  if (!stopObj) return

  const nextOrder = routeStopsList.value.length + 1
  routeStopsList.value.push({
    stopId: stopObj.id,
    stopName: stopObj.name,
    stopOrder: nextOrder,
    minutesFromStart: Number(minutesFromStartToAdd.value) || 0,
  })

  minutesFromStartToAdd.value = (Number(minutesFromStartToAdd.value) || 0) + 5
  selectedStopIdToAdd.value = null
}

const removeStopFromRoute = (index: number) => {
  routeStopsList.value.splice(index, 1)
  routeStopsList.value.forEach((item, idx) => {
    item.stopOrder = idx + 1
  })
}

const startEditRoute = async (routeSummary: RouteSummaryItem) => {
  try {
    isSavingRoute.value = true
    let detailRes: any = null

    const candidateDetailUrls = [
      `/routes/active/${encodeURIComponent(routeSummary.pathId)}`,
    ]

    for (const url of candidateDetailUrls) {
      try {
        detailRes = await apiClient.get(url)
        if (detailRes && detailRes.data) break
      } catch (e: any) {
        if (e.response?.status === 404) continue
        throw e
      }
    }

    if (!detailRes) throw new Error('Маршрут не найден на сервере')
    const data = detailRes.data

    editingRouteId.value = Number(data.id ?? data.Id ?? routeSummary.id)
    routeForm.value = {
      pathNumber: data.pathId || data.PathId || routeSummary.pathId,
      transportType: data.transportType || data.TransportType || routeSummary.transportType,
    }

    const rawSchedule = data.schedule || data.Schedule || []
    routeStopsList.value = rawSchedule.map((s: any, idx: number) => {
      let resolvedStopId = Number(s.stopId ?? s.StopId ?? s.id ?? s.Id ?? 0)
      const stopName = s.stopName || s.StopName || ''

      if (!resolvedStopId && stopsList.value.length > 0) {
        const matched = stopsList.value.find(
          (stop) =>
            (stopName && stop.name.trim().toLowerCase() === stopName.trim().toLowerCase()) ||
            (s.lat && s.lon && Math.abs(stop.lat - Number(s.lat)) < 0.0001 && Math.abs(stop.lon - Number(s.lon)) < 0.0001)
        )
        if (matched) resolvedStopId = matched.id
      }

      const minutes = Number(
        s.minutesFromStart ?? s.MinutesFromStart ?? s.minuteFromStart ?? s.MinuteFromStart ?? 0
      )

      return {
        stopId: resolvedStopId,
        stopName: stopName || (resolvedStopId ? `Остановка #${resolvedStopId}` : 'Остановка'),
        stopOrder: Number(s.stopOrder ?? s.StopOrder ?? idx + 1),
        minutesFromStart: minutes,
      }
    })

    let rawLine = data.routeLine || data.RouteLine || []
    if (typeof rawLine === 'string') {
      try { rawLine = JSON.parse(rawLine) } catch { rawLine = [] }
    }
    routePoints.value = rawLine

    activeTab.value = 'routes'
    await nextTick()
    if (!routesMapInstance) initRoutesMap()
    updatePolylineOnMap()

    if (routePolyline && routePolyline.geometry) {
      const bounds = routePolyline.geometry.getBounds()
      if (bounds) {
        routesMapInstance.setBounds(bounds, { checkZoomRange: true, zoomMargin: 60, duration: 300 })
      }
    }

    showNotice(`Маршрут ${routeSummary.pathId} загружен в конструктор`)
  } catch (err: any) {
    console.error('Ошибка загрузки деталей маршрута:', err)
    showNotice(err.response?.data?.message || 'Не удалось загрузить параметры маршрута', true)
  } finally {
    isSavingRoute.value = false
  }
}

const cancelEditRoute = () => {
  editingRouteId.value = null
  routeForm.value = { pathNumber: '', transportType: 'bus' }
  routeStopsList.value = []
  minutesFromStartToAdd.value = 0
  clearRoutePoints()
}

const handleSaveRoute = async () => {
  const pNum = routeForm.value.pathNumber.trim()
  if (!pNum) {
    showNotice('Укажите номер маршрута (например, 52к)', true)
    return
  }
  if (pNum.length > 10) {
    showNotice('Номер маршрута не может превышать 10 символов', true)
    return
  }
  if (routeStopsList.value.length < 2) {
    showNotice('Маршрут должен содержать минимум 2 остановки', true)
    return
  }
  if (routePoints.value.length < 2) {
    showNotice('Нарисуйте траекторию на карте (минимум 2 точки линии)', true)
    return
  }

  try {
    isSavingRoute.value = true

    const payload = {
      pathNumber: pNum,
      transportType: routeForm.value.transportType,
      routeLine: routePoints.value,stops: routeStopsList.value.map((s, idx) => ({
        stopId: Number(s.stopId),
        stopOrder: Number(s.stopOrder || idx + 1),
        minutesFromStart: Number(s.minutesFromStart || 0),
      })),
    }

    if (editingRouteId.value) {
      await apiClient.put(`/routes/active/${editingRouteId.value}`, payload)
      showNotice(`Маршрут ${payload.pathNumber} успешно обновлен!`)
    } else {
      await apiClient.post('/routes', payload)
      showNotice(`Маршрут ${payload.pathNumber} успешно создан!`)
    }

    cancelEditRoute()
    await fetchRoutes()
  } catch (err: any) {
    console.error('Ошибка сохранения маршрута:', err)
    showNotice(err.response?.data?.message || err.message || 'Сбой сохранения маршрута', true)
  } finally {
    isSavingRoute.value = false
  }
}

const handleConfirmDelete = async () => {
  const { type, id, title } = deleteConfirmDialog.value
  try {
    deleteConfirmDialog.value.loading = true

    if (type === 'stop') {
      await apiClient.delete(`/stops/${id}`)
      showNotice(`Остановка "${title}" удалена`)
      if (editingStopId.value === id) cancelEditStop()
      await fetchStops()
    } else {
      await apiClient.delete(`/routes/active/${id}`)
      showNotice(`Маршрут "${title}" удален`)
      if (editingRouteId.value === id) cancelEditRoute()
      await fetchRoutes()
    }

    deleteConfirmDialog.value.open = false
  } catch (err: any) {
    console.error('Ошибка при удалении:', err)
    const errorMsg = err.response?.data?.message || err.message || 'Не удалось выполнить удаление'
    showNotice(errorMsg, true)
  } finally {
    deleteConfirmDialog.value.loading = false
  }
}

// --- УМНЫЙ RESIZE OBSERVER (БЕЗ ПАРАЗИТНОГО ЦИКЛА) ---
let splitResizeObserver: ResizeObserver | null = null
let lastSplitWidth = 0

const setupSafeResizeObserver = () => {
  if (window.ResizeObserver) {
    splitResizeObserver = new ResizeObserver((entries) => {
      for (const entry of entries) {
        const currentWidth = entry.contentRect.width
        if (Math.abs(currentWidth - lastSplitWidth) > 4) {
          lastSplitWidth = currentWidth
          requestAnimationFrame(() => {
            if (activeTab.value === 'stops' && stopsMapInstance) {
              stopsMapInstance.container.fitToViewport()
            } else if (activeTab.value === 'routes' && routesMapInstance) {
              routesMapInstance.container.fitToViewport()
            }
          })
        }
      }
    })

    const rootElement = document.querySelector('.routes-admin-view')
    if (rootElement) splitResizeObserver.observe(rootElement)
  }
}

watch(activeTab, async (newTab) => {
  await nextTick()
  if (newTab === 'stops') {
    if (!stopsMapInstance) initStopsMap()
    else stopsMapInstance.container.fitToViewport()
  } else if (newTab === 'routes') {
    if (!routesMapInstance) initRoutesMap()
    else routesMapInstance.container.fitToViewport()
  }
})

onMounted(async () => {
  await fetchStops()
  await fetchRoutes()
  initStopsMap()
  setupSafeResizeObserver()
  window.addEventListener('resize', handleGlobalWindowResize)
})

const handleGlobalWindowResize = () => {
  if (stopsMapInstance) stopsMapInstance.container.fitToViewport()
  if (routesMapInstance) routesMapInstance.container.fitToViewport()
}

onUnmounted(() => {
  window.removeEventListener('resize', handleGlobalWindowResize)
  if (splitResizeObserver) splitResizeObserver.disconnect()
  if (stopsMapInstance) stopsMapInstance.destroy()
  if (routesMapInstance) routesMapInstance.destroy()
})
</script>

<template>
  <div class="routes-admin-view pa-6">
    <!-- Шапка раздела -->
    <div class="mb-4">
      <div class="d-flex align-center">
        <v-icon icon="mdi-routes" color="#00E676" size="28" class="mr-2" />
        <h1 class="text-h6 font-weight-black tracking-wider brand-title">
          КОНСТРУКТОР СЕТИ И ТОПОЛОГИИ
        </h1>
      </div>
      <p class="text-caption text-disabled font-mono mt-1">
        УПРАВЛЕНИЕ ОСТАНОВКАМИ, ТРАЕКТОРИЯМИ И РАСПИСАНИЯМИ ДВИЖЕНИЯ
      </p>
    </div>

    <!-- Вкладки управления -->
    <v-tabs v-model="activeTab" density="compact" color="#00E676" class="border-tabs mb-6 font-mono">
      <v-tab value="stops" class="text-caption font-weight-bold">
        <v-icon icon="mdi-map-marker-multiple" size="18" class="mr-2" />
        ОСТАНОВКИ ({{ stopsList.length }})
      </v-tab>
      <v-tab value="routes" class="text-caption font-weight-bold">
        <v-icon icon="mdi-vector-polyline-plus" size="18" class="mr-2" />
        МАРШРУТЫ ({{ routesList.length }})
      </v-tab>
    </v-tabs>

    <!-- ВКЛАДКА 1: ОСТАНОВКИ (СО СЛАЙДЕРОМ И ЭЛАСТИЧНОЙ КАРТОЙ) -->
    <div v-show="activeTab === 'stops'">
      <div ref="stopsSplitContainer" class="split-pane-wrapper">
        <!-- Левая часть: Таблица существующих остановок -->
        <div class="split-pane-left" :style="{ width: stopsSplitPercent + '%' }">
          <v-card class="admin-card pa-4 fill-height" rounded="0">
            <div class="d-flex align-center justify-space-between mb-3">
              <span class="text-subtitle-2 font-mono font-weight-bold text-white">
                РЕЕСТР ОСТАНОВОК
              </span>
              <div class="d-flex align-center">
                <v-text-field
                  v-model="stopsSearch"
                  prepend-inner-icon="mdi-magnify"
                  placeholder="Поиск по названию..."
                  density="compact"
                  variant="outlined"
                  color="#00E676"
                  hide-details
                  class="font-mono mr-2 search-input"
                  style="width: 200px;"
                  autocomplete="off"
                />
                <v-btn
                  icon="mdi-refresh"
                  variant="outlined"
                  size="small"
                  rounded="0"
                  :loading="isStopsLoading"
                  class="border-subtle"
                  @click="fetchStops"
                />
              </div>
            </div>

            <v-data-table
              :headers="stopTableHeaders"
              :items="stopsList"
              :search="stopsSearch"
              :loading="isStopsLoading"
              density="compact"
              class="custom-table font-mono"
              items-per-page-text="Строк:"
              no-data-text="Остановки отсутствуют"
            >
              <template #item.id="{ value }">
                <span class="text-disabled">#{{ value }}</span>
              </template>
              <template #item.name="{ item, value }">
                <span
                  class="font-weight-bold"
                  :class="editingStopId === (item as any).id ? 'text-cyan-accent-2' : 'text-white'"
                >
                  {{ value }}
                </span>
              </template>
              <template #item.lat="{ value }">
                <span class="text-green-accent-3">{{ value }}</span>
              </template>
              <template #item.lon="{ value }">
                <span class="text-green-accent-3">{{ value }}</span>
              </template>
              <!-- Кнопки действий -->
              <template #item.actions="{ item }">
                <div class="d-flex justify-center">
                  <v-btn
                    icon="mdi-pencil"
                    size="x-small"
                    variant="text"
                    color="#29B6F6"
                    class="mr-1"
                    @click="startEditStop(item as any)"
                  />
                  <v-btn
                    icon="mdi-delete"
                    size="x-small"
                    variant="text"
                    color="#FF1744"
                    @click="openDeleteConfirm('stop', (item as any).id, (item as any).name)"
                  />
                </div>
              </template>
            </v-data-table>
          </v-card>
        </div>

        <!-- Слайдер-разделитель (Stops) -->
        <div class="splitter-divider"
          :class="{ 'splitter-active': isDraggingStopsSplit }"
          @mousedown="startStopsSplitDrag"
        >
          <div class="splitter-handle" />
        </div>

        <!-- Правая часть: Форма создания/редактирования остановки (растягивается на всю высоту) -->
        <div class="split-pane-right">
          <v-card class="admin-card pa-4 stop-card-wrapper" rounded="0">
            <div class="d-flex align-center justify-space-between mb-3 flex-shrink-0">
              <span class="text-subtitle-2 font-mono font-weight-bold text-white">
                {{ editingStopId ? `РЕДАКТИРОВАНИЕ ОСТАНОВКИ #${editingStopId}` : 'НОВАЯ ОСТАНОВКА' }}
              </span>
            </div>

            <v-form class="stop-form-wrapper" @submit.prevent="handleSaveStop">
              <div class="flex-shrink-0">
                <div class="text-caption text-disabled font-mono mb-1">НАЗВАНИЕ ПУНКТА *</div>
                <v-text-field
                  v-model="stopForm.name"
                  placeholder="например: Улица Кирова, д. 12"
                  variant="outlined"
                  density="compact"
                  color="#00E676"
                  class="font-mono mb-3 input-tactical"
                  hide-details
                  autocomplete="off"
                />

                <!-- Координаты: строго без ручного ввода курсором -->
                <v-row class="mb-2">
                  <v-col cols="6">
                    <div class="text-caption text-disabled font-mono mb-1">ШИРОТА (LAT)</div>
                    <v-text-field
                      :model-value="stopForm.lat ?? ''"
                      placeholder="Кликните на карте"
                      variant="outlined"
                      density="compact"
                      color="#00E676"
                      readonly
                      tabindex="-1"
                      class="font-mono input-tactical input-no-cursor"
                      hide-details
                    />
                  </v-col>
                  <v-col cols="6">
                    <div class="text-caption text-disabled font-mono mb-1">ДОЛГОТА (LON)</div>
                    <v-text-field
                      :model-value="stopForm.lon ?? ''"
                      placeholder="Кликните на карте"
                      variant="outlined"
                      density="compact"
                      color="#00E676"
                      readonly
                      tabindex="-1"
                      class="font-mono input-tactical input-no-cursor"
                      hide-details
                    />
                  </v-col>
                </v-row>

                <div class="text-caption text-disabled font-mono mb-1 d-flex align-center justify-space-between">
                  <span>ВЫБЕРИТЕ ТОЧКУ НА КАРТЕ:</span>
                  <span v-if="stopForm.lat" class="text-green-accent-3">ТОЧКА ВЫБРАНА</span>
                </div>
              </div>

              <!-- Растягивающаяся карта остановок -->
              <div class="map-picker-container mb-3">
                <div ref="stopsMapContainer" class="embedded-yandex-map" />
              </div>

              <!-- Кнопки сохранения и отмены (ровно в ряд) -->
              <div class="d-flex ga-2 flex-shrink-0">
                <v-btn
                  v-if="editingStopId"
                  variant="outlined"
                  color="#FFD600"
                  rounded="0"
                  height="40"
                  class="font-mono px-4"
                  @click="cancelEditStop"
                >
                  ОТМЕНА
                </v-btn>
                <v-btn
                  type="submit"
                  :color="editingStopId ? '#29B6F6' : '#00E676'"
                  rounded="0"
                  height="40"
                  :loading="isSavingStop"
                  :disabled="!stopForm.name.trim() || !stopForm.lat"
                  class="font-mono font-weight-bold text-black flex-grow-1"
                >
                  {{ editingStopId ? 'СОХРАНИТЬ ИЗМЕНЕНИЯ' : 'ДОБАВИТЬ ОСТАНОВКУ' }}
                </v-btn>
              </div>
            </v-form>
          </v-card>
        </div>
      </div>
    </div>

    <!-- ВКЛАДКА 2: МАРШРУТЫ И КОНСТРУКТОР (СО СЛАЙДЕРОМ) -->
    <div v-show="activeTab === 'routes'">
      <div ref="routesSplitContainer" class="split-pane-wrapper">
        <!-- Левая часть: Реестр + Параметры + Остановки -->
        <div class="split-pane-left" :style="{ width: routesSplitPercent + '%' }">
          <v-card class="admin-card pa-4 mb-4" rounded="0">
            <div class="d-flex align-center justify-space-between mb-2">
              <span class="text-subtitle-2 font-mono font-weight-bold text-white">
                РЕЕСТР ДЕЙСТВУЮЩИХ МАРШРУТОВ
              </span>
              <div class="d-flex align-center">
                <v-text-field
                  v-model="routesSearch"
                  prepend-inner-icon="mdi-magnify"
                  placeholder="Поиск маршрута..."
                  density="compact"
                  variant="outlined"
                  color="#00E676"
                  hide-details
                  class="font-mono mr-2 search-input"
                  style="width: 170px;"
                  autocomplete="off"
                />
                <v-btn
                  icon="mdi-refresh"
                  variant="outlined"
                  size="x-small"
                  rounded="0"
                  :loading="isRoutesLoading"
                  class="border-subtle"
                  @click="fetchRoutes"
                />
              </div>
            </div>

            <v-data-table
              :headers="routeTableHeaders"
              :items="routesList"
              :search="routesSearch"
              :loading="isRoutesLoading"
              density="compact"
              class="custom-table font-mono"
              items-per-page="5"
              items-per-page-text="Строк:"
              no-data-text="Маршруты отсутствуют"
            >
              <template #item.id="{ value }">
                <span class="text-disabled">#{{ value }}</span>
              </template>
              <template #item.pathId="{ item, value }">
                <span
                  class="font-weight-bold"
                  :class="editingRouteId === (item as any).id ? 'text-cyan-accent-2' : 'text-white'"
                >
                  {{ value?.toUpperCase() }}
                </span>
              </template>
              <template #item.transportType="{ value }">
                <span class="text-capitalize text-disabled">
                  {{
                    value === 'bus' ? 'Автобус' :
                    value === 'trolleybus' ? 'Троллейбус' :
                    value === 'tram' ? 'Трамвай' :
                    value === 'minibus' ? 'Маршрутка' : value
                  }}
                </span>
              </template>
              <template #item.actions="{ item }">
                <div class="d-flex justify-center">
                  <v-btn
                    icon="mdi-pencil"
                    size="x-small"
                    variant="text"
                    color="#29B6F6"
                    class="mr-1"
                    @click="startEditRoute(item as any)"
                  />
                  <v-btn
                    icon="mdi-delete"
                    size="x-small"
                    variant="text"
                    color="#FF1744"
                    @click="openDeleteConfirm('route', (item as any).id, (item as any).pathId)"
                  />
                </div>
              </template>
            </v-data-table>
          </v-card>

          <!-- Параметры маршрута -->
          <v-card class="admin-card pa-4 mb-4" rounded="0">
            <div class="mb-3">
              <span class="text-subtitle-2 font-mono font-weight-bold text-white">
                {{ editingRouteId ? `РЕДАКТИРОВАНИЕ МАРШРУТА #${editingRouteId}` : '1. ПАРАМЕТРЫ МАРШРУТА' }}
              </span>
            </div>

            <v-row>
              <v-col cols="6">
                <div class="text-caption text-disabled font-mono mb-1">НОМЕР МАРШРУТА (МАКС. 10 СИМВ.) *</div>
                <v-text-field
                  v-model="routeForm.pathNumber"
                  placeholder="52к"
                  variant="outlined"
                  density="compact"
                  color="#00E676"
                  class="font-mono input-tactical"
                  hide-details
                  maxlength="10"
                  counter="10"
                  autocomplete="off"
                />
              </v-col>
              <v-col cols="6">
                <div class="text-caption text-disabled font-mono mb-1">ТИП ТРАНСПОРТА</div>
                <v-select
                  v-model="routeForm.transportType"
                  :items="transportTypeOptions"
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
          </v-card>

          <!-- Цепочка остановок -->
          <v-card class="admin-card pa-4" rounded="0">
            <div class="text-subtitle-2 font-mono font-weight-bold text-white mb-1 d-flex justify-space-between">
              <span>2. ПОСЛЕДОВАТЕЛЬНОСТЬ ОСТАНОВОК</span>
              <span class="text-green-accent-3 font-mono">ВЫБРАНО: {{ routeStopsList.length }}</span>
            </div>
            <p class="text-caption text-disabled font-mono mb-3" style="font-size: 11px !important;">
              Укажите остановки по порядку. Счётчик минут — это время в пути от 1-й (начальной) остановки (0 для старта).
            </p>

            <div class="d-flex align-center mb-3">
              <v-autocomplete
                v-model="selectedStopIdToAdd"
                :items="stopsList"
                item-title="name"
                item-value="id"
                placeholder="Выберите остановку..."
                density="compact"
                variant="outlined"
                color="#00E676"
                hide-details
                class="font-mono input-tactical flex-grow-1 mr-2"
              />
              <v-tooltip text="Минуты в пути от первой (начальной) остановки маршрута" location="top">
                <template #activator="{ props }">
                  <v-text-field
                    v-bind="props"
                    v-model.number="minutesFromStartToAdd"
                    type="number"
                    placeholder="Мин."
                    density="compact"
                    variant="outlined"
                    color="#00E676"
                    hide-details
                    style="max-width: 95px;"
                    class="font-mono input-tactical mr-2"
                  />
                </template>
              </v-tooltip>
              <v-btn
                color="#00E676"
                variant="tonal"
                rounded="0"
                height="40"
                class="font-mono"
                :disabled="!selectedStopIdToAdd"
                @click="addStopToRoute"
              >
                +
              </v-btn>
            </div>

            <!-- Список выбранных остановок -->
            <div class="route-stops-scroll-area mb-4">
              <div
                v-for="(st, idx) in routeStopsList"
                :key="st.stopId + '-' + idx"
                class="route-stop-draft-row pa-2 mb-1 d-flex align-center justify-space-between"
              >
                <div class="d-flex align-center">
                  <span class="stop-order-chip mr-2">{{ st.stopOrder }}</span>
                  <span class="text-caption font-mono text-white line-clamp-1">{{ st.stopName }}</span>
                </div>
                <div class="d-flex align-center">
                  <span class="text-caption font-mono text-disabled mr-3">+{{ st.minutesFromStart }} мин от 1-й ост.</span>
                  <v-btn
                    icon="mdi-trash-can-outline"
                    variant="text"
                    size="x-small"
                    color="#FF1744"
                    rounded="0"
                    @click="removeStopFromRoute(idx)"
                  />
                </div>
              </div>

              <div v-if="routeStopsList.length === 0" class="text-caption font-mono text-disabled text-center py-4">
                Добавьте минимум 2 остановки следования
              </div>
            </div>

            <!-- Кнопки -->
            <div class="d-flex ga-2">
              <v-btn
                v-if="editingRouteId"
                variant="outlined"
                color="#FFD600"
                rounded="0"
                height="42"
                class="font-mono px-5"
                @click="cancelEditRoute"
              >
                ОТМЕНА
              </v-btn>
              <v-btn
                :color="editingRouteId ? '#29B6F6' : '#00E676'"
                variant="flat"
                rounded="0"
                height="42"
                :loading="isSavingRoute"
                :disabled="!routeForm.pathNumber.trim() || routeStopsList.length < 2 || routePoints.length < 2"
                class="font-mono font-weight-bold text-black flex-grow-1"
                @click="handleSaveRoute"
              >
                {{ editingRouteId ? 'СОХРАНИТЬ МАРШРУТ' : 'СОЗДАТЬ МАРШРУТ' }}
              </v-btn>
            </div>
          </v-card>
        </div>

        <!-- Слайдер-разделитель (Routes) -->
        <div
          class="splitter-divider"
          :class="{ 'splitter-active': isDraggingRoutesSplit }"
          @mousedown="startRoutesSplitDrag"
        >
          <div class="splitter-handle" />
        </div>

        <!-- Правая часть: Карта со строгой фиксацией flexbox-высоты -->
        <div class="split-pane-right">
          <v-card class="admin-card pa-4 map-card-wrapper" rounded="0">
            <div class="d-flex align-center justify-space-between mb-3 map-card-header">
              <div>
                <span class="text-subtitle-2 font-mono font-weight-bold text-white">
                  3. ТРАЕКТОРИЯ ЛИНИИ НА КАРТЕ
                </span>
                <span class="text-caption font-mono text-disabled ml-2">
                  (Кликайте по дорогам для добавления точек)
                </span>
              </div>
              <div class="d-flex align-center">
                <span class="text-caption font-mono mr-3 text-cyan-accent-3 font-weight-bold">
                  ТОЧЕК: {{ routePoints.length }}
                </span>
                <v-btn
                  variant="outlined"
                  size="small"
                  rounded="0"
                  color="#FFD600"
                  class="font-mono mr-2"
                  :disabled="routePoints.length === 0"
                  @click="removeLastRoutePoint"
                >
                  <v-icon icon="mdi-undo" size="16" class="mr-1" />
                  ШАГ НАЗАД
                </v-btn>
                <v-btn
                  variant="outlined"
                  size="small"
                  rounded="0"
                  color="#FF1744"
                  class="font-mono"
                  :disabled="routePoints.length === 0"
                  @click="clearRoutePoints"
                >
                  <v-icon icon="mdi-delete-sweep" size="16" class="mr-1" />
                  ОЧИСТИТЬ
                </v-btn>
              </div>
            </div>

            <div class="map-route-builder-container">
              <div ref="routesMapContainer" class="embedded-yandex-map" />
            </div></v-card>
        </div>
      </div>
    </div>

    <!-- МОДАЛЬНОЕ ОКНО ПОДТВЕРЖДЕНИЯ УДАЛЕНИЯ -->
    <v-dialog v-model="deleteConfirmDialog.open" max-width="440" persistent transition="fade-transition">
      <v-card class="delete-dialog-card pa-5" rounded="0">
        <div class="d-flex align-center mb-3">
          <v-icon icon="mdi-alert-octagon-outline" color="#FF1744" size="26" class="mr-2" />
          <div class="text-subtitle-2 font-weight-bold font-mono text-white">
            ПОДТВЕРЖДЕНИЕ УДАЛЕНИЯ
          </div>
        </div>

        <v-divider class="border-opacity-10 mb-4" />

        <div class="text-body-2 font-mono text-grey-lighten-2 mb-5">
          Вы действительно хотите удалить {{ deleteConfirmDialog.type === 'stop' ? 'остановку' : 'маршрут' }}
          <span class="text-red-accent-2 font-weight-bold">"{{ deleteConfirmDialog.title }}"</span>?
        </div>

        <div class="d-flex justify-end">
          <v-btn
            variant="text"
            size="small"
            rounded="0"
            class="font-mono mr-2"
            :disabled="deleteConfirmDialog.loading"
            @click="deleteConfirmDialog.open = false"
          >
            ОТМЕНА
          </v-btn>
          <v-btn
            color="#FF1744"
            variant="flat"
            size="small"
            rounded="0"
            class="font-mono font-weight-bold text-white px-4"
            :loading="deleteConfirmDialog.loading"
            @click="handleConfirmDelete"
          >
            УДАЛИТЬ
          </v-btn>
        </div>
      </v-card>
    </v-dialog>

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
.routes-admin-view {
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

.admin-card {
  background-color: #161B22 !important;
  border: 1px solid #30363D !important;
}

.search-input {
  background-color: #0E1116;
}

.border-subtle {
  border-color: #30363D !important;
}

.input-tactical {
  background-color: #0E1116;
}

/* Полный запрет курсора и клика по координатам */
.input-no-cursor :deep(input) {
  cursor: not-allowed !important;
  pointer-events: none !important;
  user-select: none !important;
}

/* --- СЛАЙДЕРЫ-РАЗДЕЛИТЕЛИ (SPLIT-PANES) --- */
.split-pane-wrapper {
  display: flex;
  width: 100%;
  position: relative;
  min-height: 620px;
}

.split-pane-left {
  min-width: 320px;
  flex-shrink: 0;
}

.split-pane-right {
  flex-grow: 1;
  min-width: 320px;
  width: 0;
  display: flex;
}

.splitter-divider {
  width: 12px;
  cursor: col-resize;
  display: flex;
  align-items: center;
  justify-content: center;
  user-select: none;
  background-color: transparent;
  transition: background-color 0.15s ease;
  margin: 0 2px;
  z-index: 10;
}

.splitter-divider:hover,
.splitter-active {
  background-color: rgba(0, 230, 118, 0.08);
}

.splitter-handle {
  width: 4px;
  height: 48px;
  background-color: #30363D;
  border-radius: 2px;
  transition: all 0.2s ease;
}

.splitter-divider:hover .splitter-handle,
.splitter-active .splitter-handle {
  background-color: #00E676;
  box-shadow: 0 0 8px rgba(0, 230, 118, 0.6);
  height: 64px;
}

/* --- КАРТОЧКА И РАСТЯГИВАЮЩАЯСЯ КАРТА ОСТАНОВОК --- */
.stop-card-wrapper {
  display: flex !important;
  flex-direction: column !important;
  height: 100% !important;
  width: 100% !important;
  overflow: hidden !important;
}

.stop-form-wrapper {
  display: flex !important;
  flex-direction: column !important;
  flex: 1 1 0px !important;
  height: 100% !important;
  min-height: 0 !important;
}

.map-picker-container {
  flex: 1 1 0px !important;
  min-height: 360px;
  height: 100% !important;
  width: 100%;
  border: 1px solid #30363D;
  position: relative;
  overflow: hidden;
}

/* --- КАРТОЧКА И РАСТЯГИВАЮЩАЯСЯ КАРТА МАРШРУТОВ --- */
.map-card-wrapper {
  display: flex !important;
  flex-direction: column !important;
  height: 100% !important;
  width: 100% !important;
  overflow: hidden !important;
}

.map-card-header {
  flex-shrink: 0;
}

.map-route-builder-container {
  flex: 1 1 0px !important;
  min-height: 460px;
  height: 100% !important;
  width: 100%;
  border: 1px solid #30363D;
  position: relative;
  overflow: hidden;
}

.embedded-yandex-map {
  width: 100%;
  height: 100%;
}

/* Стилизация тайлов карт в темную тему */
:deep(.ymaps-2-1-79-map),
:deep(.ymaps-2-1-79-islets_map) {
  filter: invert(90%) hue-rotate(180%) contrast(90%) brightness(95%);
}

/* Скролл остановок */
.route-stops-scroll-area {
  max-height: 220px;
  overflow-y: auto;
  border: 1px solid #21262D;
  background-color: #0E1116;
  padding: 4px;
}

.route-stop-draft-row {
  background-color: #161B22;
  border: 1px solid #30363D;
}

.stop-order-chip {
  background-color: #21262D;
  border: 1px solid #30363D;
  padding: 1px 6px;
  border-radius: 2px;
  font-size: 11px;
  font-weight: bold;
  color: #00E676;
}

/* Диалог удаления */
.delete-dialog-card {
  background-color: #161B22 !important;
  border: 1px solid #FF1744 !important;
  box-shadow: 0 0 24px rgba(255, 23, 68, 0.2) !important;
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