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

// Режим редактирования остановки
const editingStopId = ref<number | null>(null)

// Форма остановки
const stopForm = ref({
  name: '',
  lat: null as number | null,
  lon: null as number | null,
})
const isSavingStop = ref<boolean>(false)

// Карта для пикинга координат остановки
const stopsMapContainer = ref<HTMLDivElement | null>(null)
let stopsMapInstance: any = null
let tempStopPlacemark: any = null

const stopTableHeaders = [
  { title: 'ID', key: 'id', align: 'start' as const, width: '70px' },
  { title: 'НАЗВАНИЕ ОСТАНОВКИ', key: 'name', align: 'start' as const },
  { title: 'ШИРОТА (LAT)', key: 'lat', align: 'start' as const },
  { title: 'ДОЛГОТА (LON)', key: 'lon', align: 'start' as const },
  { title: 'ДЕЙСТВИЯ', key: 'actions', align: 'center' as const, sortable: false, width: '110px' },
]

// Загрузка реестра остановок
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

// Инициализация карты выбора координат для остановки
const initStopsMap = () => {
  if (typeof ymaps === 'undefined' || !stopsMapContainer.value || stopsMapInstance) return

  ymaps.ready(() => {
    stopsMapInstance = new ymaps.Map(
      stopsMapContainer.value,
      {
        center: [55.4311, 37.5458], // Подольск
        zoom: 13,
        controls: ['zoomControl'],
      },
      { suppressMapOpenBlock: true }
    )

    // Клик по карте заполняет координаты
    stopsMapInstance.events.add('click', (e: any) => {
      const coords = e.get('coords') // [lat, lon]
      stopForm.value.lat = Number(coords[0].toFixed(6))
      stopForm.value.lon = Number(coords[1].toFixed(6))

      updateStopMarker(coords)
    })
  })
}

// Обновление метки на мини-карте остановок
const updateStopMarker = (coords: [number, number]) => {
  if (!stopsMapInstance) return
  if (tempStopPlacemark){
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

// Старт редактирования остановки
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

// Отмена редактирования остановки
const cancelEditStop = () => {
  editingStopId.value = null
  stopForm.value = { name: '', lat: null, lon: null }
  if (tempStopPlacemark && stopsMapInstance) {
    stopsMapInstance.geoObjects.remove(tempStopPlacemark)
    tempStopPlacemark = null
  }
}

// Сохранение (POST или PUT) остановки
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
      // РЕДАКТИРОВАНИЕ (PUT)
      await apiClient.put(`/stops/${editingStopId.value}`, payload)
      showNotice(`Остановка "${payload.name}" успешно обновлена!`)
    } else {
      // СОЗДАНИЕ (POST)
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

// Режим редактирования маршрута
const editingRouteId = ref<number | null>(null)

const routeForm = ref({
  pathNumber: '',
  transportType: 'bus',
})

const transportTypeOptions = [
  { title: 'Автобус', value: 'bus' },
  { title: 'Трамвай', value: 'tram' },
  { title: 'Маршрутка', value: 'minibus' },
]

// Добавление остановок в маршрут
const selectedStopIdToAdd = ref<number | null>(null)
const minutesFromStartToAdd = ref<number>(0)
const routeStopsList = ref<RouteStopDraft[]>([])

// Интерактивное рисование линии
const routesMapContainer = ref<HTMLDivElement | null>(null)
let routesMapInstance: any = null
let routePolyline: any = null
const routePoints = ref<[number, number][]>([])
const isSavingRoute = ref<boolean>(false)

const routeTableHeaders = [
  { title: 'ID', key: 'id', align: 'start' as const, width: '60px' },
  { title: 'НОМЕР', key: 'pathId', align: 'start' as const },
  { title: 'ТИП ТРАНСПОРТА', key: 'transportType', align: 'start' as const },
  { title: 'ДЕЙСТВИЯ', key: 'actions', align: 'center' as const, sortable: false, width: '100px' },
]

// Загрузка реестра существующих маршрутов
const fetchRoutes = async () => {
  try {
    isRoutesLoading.value = true
    const res = await apiClient.get('/routes/active')
    const raw = Array.isArray(res.data) ? res.data : []

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

// Инициализация карты-конструктора
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

    // Клик добавляет точку в Polyline
    routesMapInstance.events.add('click', (e: any) => {
      const coords = e.get('coords')
      const fixedPoint: [number, number] = [Number(coords[0].toFixed(6)), Number(coords[1].toFixed(6))]
      routePoints.value.push(fixedPoint)
      updatePolylineOnMap()
    })
  })
}

// Обновление Polyline на карте
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

// Отмена последней точки линии
const removeLastRoutePoint = () => {
  if (routePoints.value.length > 0) {
    routePoints.value.pop()
    updatePolylineOnMap()
  }
}

// Очистка всех точек линии
const clearRoutePoints = () => {
  routePoints.value = []
  updatePolylineOnMap()
}

// Добавление остановки в маршрут
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

// Удаление остановки из списка
const removeStopFromRoute = (index: number) => {
  routeStopsList.value.splice(index, 1)
  routeStopsList.value.forEach((item, idx) => {
    item.stopOrder = idx + 1
  })
}

// Старт редактирования маршрута
const startEditRoute = async (routeSummary: RouteSummaryItem) => {
  try {
    isSavingRoute.value = true
    const detailRes = await apiClient.get(`/routes/active/${encodeURIComponent(routeSummary.pathId)}`)
    const data = detailRes.data

    editingRouteId.value = Number(data.id ?? data.Id ?? routeSummary.id)
    routeForm.value = {
      pathNumber: data.pathId || data.PathId || routeSummary.pathId,
      transportType: data.transportType || data.TransportType || routeSummary.transportType,
    }

    // Восстанавливаем расписание остановок
    const rawSchedule = data.schedule || data.Schedule || []
    routeStopsList.value = rawSchedule.map((s: any, idx: number) => ({
      stopId: Number(s.stopId ?? s.StopId ?? 0),
      stopName: s.stopName || s.StopName || `Остановка #${s.stopId}`,
      stopOrder: Number(s.stopOrder ?? s.StopOrder ?? idx + 1),
      minutesFromStart: Number(s.minutesFromStart ?? s.MinutesFromStart ?? 0),
    }))

    // Восстанавливаем геометрию нити
    let rawLine = data.routeLine || data.RouteLine || []
    if (typeof rawLine === 'string') {
      try { rawLine = JSON.parse(rawLine) } catch { rawLine = [] }
    }
    routePoints.value = rawLine

    // Переключаемся на таб маршрутов и фокусируем карту
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

// Отмена редактирования маршрута
const cancelEditRoute = () => {
  editingRouteId.value = null
  routeForm.value = { pathNumber: '', transportType: 'bus' }
  routeStopsList.value = []
  minutesFromStartToAdd.value = 0
  clearRoutePoints()
}

// Сохранение (POST или PUT) маршрута
const handleSaveRoute = async () => {
  if (!routeForm.value.pathNumber.trim()) {
    showNotice('Укажите номер маршрута (например, 52к)', true)
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
      pathNumber: routeForm.value.pathNumber.trim(),
      transportType: routeForm.value.transportType,
      routeLine: routePoints.value,
      stops: routeStopsList.value.map((s) => ({
        stopId: s.stopId,
        stopOrder: s.stopOrder,
        minutesFromStart: s.minutesFromStart,
      })),
    }

    if (editingRouteId.value) {
      // РЕДАКТИРОВАНИЕ (PUT)
      await apiClient.put(`/routes/${editingRouteId.value}`, payload)
      showNotice(`Маршрут ${payload.pathNumber} успешно обновлен!`)
    } else {
      // СОЗДАНИЕ (POST)
      await apiClient.post('/routes', payload)
      showNotice(`Маршрут ${payload.pathNumber} успешно создан и запущен!`)
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

// ==============================================================
// ОБЩЕЕ УДАЛЕНИЕ (DELETE)
// ==============================================================
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
      await apiClient.delete(`/routes/${id}`)
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

// Переключение табов и обновление карт
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
})

onUnmounted(() => {
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
        УПРАВЛЕНИЕ ОСТАНОВКАМИ, ТРАЕКТОРИЯМИ И РАСПИСАНИЯМИ ДВИЖЕНИЯ</p>
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

    <!-- ВКЛАДКА 1: ОСТАНОВКИ -->
    <div v-show="activeTab === 'stops'">
      <v-row>
        <!-- Левая колонка: Таблица существующих остановок с действиями -->
        <v-col cols="12" md="7">
          <v-card class="admin-card pa-4" rounded="0">
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
                  style="width: 220px;"
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
              <!-- Кнопки действий над остановкой -->
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
        </v-col>

        <!-- Правая колонка: Форма создания/редактирования остановки -->
        <v-col cols="12" md="5">
          <v-card class="admin-card pa-4" rounded="0">
            <div class="d-flex align-center justify-space-between mb-3">
              <span class="text-subtitle-2 font-mono font-weight-bold text-white">
                {{ editingStopId ? `РЕДАКТИРОВАНИЕ ОСТАНОВКИ #${editingStopId}` : 'НОВАЯ ОСТАНОВКА' }}
              </span>
              <v-btn
                v-if="editingStopId"
                variant="text"
                size="x-small"
                color="#FFD600"
                class="font-mono"
                @click="cancelEditStop"
              >
                ОТМЕНА
              </v-btn>
            </div>

            <v-form @submit.prevent="handleSaveStop">
              <div class="text-caption text-disabled font-mono mb-1">НАЗВАНИЕ ПУНКТА *</div>
              <v-text-field
                v-model="stopForm.name"
                placeholder="например: Улица Кирова, д. 12"
                variant="outlined"
                density="compact"
                color="#00E676"
                class="font-mono mb-3 input-tactical"
                hide-details
              />

              <v-row class="mb-2">
                <v-col cols="6">
                  <div class="text-caption text-disabled font-mono mb-1">ШИРОТА (LAT)</div>
                  <v-text-field
                    v-model.number="stopForm.lat"
                    placeholder="55.xxxxxx"
                    variant="outlined"
                    density="compact"
                    color="#00E676"
                    readonly
                    class="font-mono input-tactical"
                    hide-details
                  />
                </v-col>
                <v-col cols="6">
                  <div class="text-caption text-disabled font-mono mb-1">ДОЛГОТА (LON)</div>
                  <v-text-field
                    v-model.number="stopForm.lon"
                    placeholder="37.xxxxxx"
                    variant="outlined"
                    density="compact"
                    color="#00E676"
                    readonly
                    class="font-mono input-tactical"
                    hide-details
                  />
                </v-col>
              </v-row>

              <!-- Карта-пикер -->
              <div class="text-caption text-disabled font-mono mb-1 d-flex align-center justify-space-between">
                <span>КЛИКНИТЕ ПО КАРТЕ ДЛЯ ВЫБОРА КООРДИНАТ:</span>
                <span v-if="stopForm.lat" class="text-green-accent-3">ТОЧКА ВЫБРАНА</span>
              </div>
              <div class="map-picker-container mb-4">
                <div ref="stopsMapContainer" class="embedded-yandex-map" />
              </div>

              <div class="d-flex">
                <v-btn
                  v-if="editingStopId"
                  variant="outlined"
                  color="#FFD600"
                  rounded="0"
                  height="40"
                  class="font-mono mr-2"
                  @click="cancelEditStop"
                >
                  ОТМЕНА
                </v-btn>
                <v-btn
                  type="submit"
                  block
                  :color="editingStopId ? '#29B6F6' : '#00E676'"
                  rounded="0"
                  height="40"
                  :loading="isSavingStop"
                  :disabled="!stopForm.name.trim() || !stopForm.lat"
                  class="font-mono font-weight-bold text-black"
                >
                  {{ editingStopId ? 'СОХРАНИТЬ ИЗМЕНЕНИЯ' : 'ДОБАВИТЬ ОСТАНОВКУ' }}
                </v-btn>
              </div>
            </v-form>
          </v-card>
        </v-col>
      </v-row>
    </div>

    <!-- ВКЛАДКА 2: МАРШРУТЫ И КОНСТРУКТОР -->
    <div v-show="activeTab === 'routes'">
      <v-row>
        <!-- Левая колонка: Реестр маршрутов + Параметры + Остановки -->
        <v-col cols="12" md="5">
          <!-- Реестр существующих маршрутов -->
          <v-card class="admin-card pa-4 mb-4" rounded="0">
            <div class="d-flex align-center justify-space-between mb-2">
              <span class="text-subtitle-2 font-mono font-weight-bold text-white">
                РЕЕСТР ДЕЙСТВУЮЩИХ МАРШРУТОВ
              </span>
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
                <span class="text-capitalize text-disabled">{{ value }}</span>
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
            <div class="d-flex align-center justify-space-between mb-3">
              <span class="text-subtitle-2 font-mono font-weight-bold text-white">
                {{ editingRouteId ? `РЕДАКТИРОВАНИЕ МАРШРУТА #${editingRouteId}` : '1. ПАРАМЕТРЫ МАРШРУТА' }}
              </span>
              <v-btn
                v-if="editingRouteId"
                variant="text"
                size="x-small"
                color="#FFD600"
                class="font-mono"
                @click="cancelEditRoute"
              >
                ОТМЕНА
              </v-btn>
            </div>

            <v-row>
              <v-col cols="6">
                <div class="text-caption text-disabled font-mono mb-1">НОМЕР МАРШРУТА *</div>
                <v-text-field
                  v-model="routeForm.pathNumber"
                  placeholder="52к"
                  variant="outlined"
                  density="compact"
                  color="#00E676"
                  class="font-mono input-tactical"
                  hide-details
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
            <div class="text-subtitle-2 font-mono font-weight-bold text-white mb-3 d-flex justify-space-between">
              <span>2. ПОСЛЕДОВАТЕЛЬНОСТЬ ОСТАНОВОК</span>
              <span class="text-green-accent-3 font-mono">ВЫБРАНО: {{ routeStopsList.length }}</span>
            </div>

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
                class="font-mono input-tactical mr-2"
              />
              <v-text-field
                v-model.number="minutesFromStartToAdd"
                type="number"
                placeholder="Мин."
                density="compact"
                variant="outlined"
                color="#00E676"
                hide-details
                style="max-width: 90px;"
                class="font-mono input-tactical mr-2"
              />
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
                  <span class="text-caption font-mono text-disabled mr-3">+{{ st.minutesFromStart }} мин</span>
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

            <div class="d-flex">
              <v-btn
                v-if="editingRouteId"
                variant="outlined"
                color="#FFD600"
                rounded="0"
                height="42"
                class="font-mono mr-2"
                @click="cancelEditRoute"
              >
                ОТМЕНА
              </v-btn>
              <v-btn
                block
                :color="editingRouteId ? '#29B6F6' : '#00E676'"
                rounded="0"
                height="42"
                :loading="isSavingRoute"
                :disabled="!routeForm.pathNumber.trim() || routeStopsList.length < 2 || routePoints.length < 2"
                class="font-mono font-weight-bold text-black"
                @click="handleSaveRoute"
              >
                {{ editingRouteId ? 'СОХРАНИТЬ МАРШРУТ' : 'СОЗДАТЬ И ЗАПУСТИТЬ МАРШРУТ' }}
              </v-btn>
            </div>
          </v-card>
        </v-col>

        <!-- Правая колонка: Карта для прокладки траектории -->
        <v-col cols="12" md="7">
          <v-card class="admin-card pa-4" rounded="0">
            <div class="d-flex align-center justify-space-between mb-3">
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
            </div>
          </v-card>
        </v-col>
      </v-row>
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
            rounded="0"
            size="small"
            class="font-mono mr-2"
            :disabled="deleteConfirmDialog.loading"
            @click="deleteConfirmDialog.open = false"
          >
            ОТМЕНА
          </v-btn>
          <v-btn
            color="#FF1744"
            variant="flat"
            rounded="0"
            size="small"
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
  background-color:#0A0C10;
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

/* Контейнеры встроенных карт */
.map-picker-container {
  height: 320px;
  width: 100%;
  border: 1px solid #30363D;
  position: relative;
  overflow: hidden;
}

.map-route-builder-container {
  height: 600px;
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

/* Скролл последовательности остановок */
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