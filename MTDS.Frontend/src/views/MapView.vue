<script setup lang="ts">
import { onMounted, onUnmounted, ref, computed } from 'vue'
import apiClient from '../api'

declare const ymaps: any

// --- ИНТЕРФЕЙСЫ ДАННЫХ ---
interface StopArrival {
  pathId: string
  transportType: string
  arrivalTime?: string
}

interface UnifiedStop {
  key: string
  name: string
  lat: number
  lon: number
  routes: StopArrival[]
}

interface DetailedRoute {
  pathId: string
  transportType: string
  routeLine: [number, number][]
  schedule: Array<{
    stopName: string
    lat: number
    lon: number
    arrivalTime?: string
  }>
}

// --- СОСТОЯНИЕ КОМПОНЕНТА ---
const mapContainer = ref<HTMLDivElement | null>(null)
const isLoading = ref<boolean>(true)
const errorMessage = ref<string | null>(null)

// Данные
const allRoutes = ref<DetailedRoute[]>([])
const unifiedStops = ref<UnifiedStop[]>([])
const routeSearch = ref<string>('')
const isSidebarOpen = ref<boolean>(true)

// Состояние фокуса и выбора
const selectedStop = ref<UnifiedStop | null>(null)
const focusedRoute = ref<DetailedRoute | null>(null)

// Инстансы Яндекс.Карт
let yandexMapInstance: any = null
const stopPlacemarkMap = new Map<string, any>()
let activePolyline: any = null

// --- ХЕЛПЕРЫ ЦВЕТОВ И ИКОНОК ---
const getRouteColor = (type?: string): string => {
  const t = (type || '').toLowerCase()
  if (t.includes('tram') || t.includes('трам')) return '#FF5252' // Трамвай: Красный/Оранжевый
  if (t.includes('mini') || t.includes('марш')) return '#FFB300' // Маршрутка: Желтый/Янтарный
  return '#29B6F6' // Автобус: Синий
}

const getTransportIcon = (type?: string): string => {
  const t = (type || '').toLowerCase()
  if (t.includes('tram') || t.includes('трам')) return 'mdi-tram'
  if (t.includes('mini') || t.includes('марш')) return 'mdi-van-utility'
  return 'mdi-bus'
}

// Форматирование времени прибытия (без отрицательных минут!)
const formatArrivalTime = (arrivalTimeStr?: string): { label: string; isUpcoming: boolean } => {
  if (!arrivalTimeStr) return { label: 'по расп.', isUpcoming: false }
  
  const arrivalDate = new Date(arrivalTimeStr)
  if (isNaN(arrivalDate.getTime())) return { label: 'по расп.', isUpcoming: false }

  const now = new Date()
  const diffMinutes = Math.floor((arrivalDate.getTime() - now.getTime()) / 60000)

  const hours = String(arrivalDate.getHours()).padStart(2, '0')
  const minutes = String(arrivalDate.getMinutes()).padStart(2, '0')
  const timeFormatted = `${hours}:${minutes}`

  if (diffMinutes > 0 && diffMinutes <= 120) {
    return { label: `через ${diffMinutes} мин (${timeFormatted})`, isUpcoming: true }
  } else {
    return { label: `${timeFormatted} (по расп.)`, isUpcoming: false }
  }
}

// Фильтр списка маршрутов для боковой панели
const filteredRoutes = computed(() => {
  if (!routeSearch.value.trim()) return allRoutes.value
  const q = routeSearch.value.toLowerCase().trim()
  return allRoutes.value.filter(
    (r) => r.pathId.toLowerCase().includes(q) || r.transportType.toLowerCase().includes(q)
  )
})

// Маршруты выбранной остановки, отсортированные по времени
const sortedStopRoutes = computed(() => {
  if (!selectedStop.value) return []
  return [...selectedStop.value.routes].sort((a, b) => {
    if (!a.arrivalTime) return 1
    if (!b.arrivalTime) return -1
    return new Date(a.arrivalTime).getTime() - new Date(b.arrivalTime).getTime()
  })
})

// --- ИНИЦИАЛИЗАЦИЯ КАРТЫ ---
const initMap = (): Promise<void> => {
  return new Promise((resolve, reject) => {
    if (typeof ymaps === 'undefined') {
      return reject(new Error('Скрипт Яндекс.Карт не загружен. Проверьте index.html.'))
    }

    ymaps.ready(() => {
      try {
        yandexMapInstance = new ymaps.Map(
          mapContainer.value,
          {
            center: [55.4311, 37.5458], // Подольск / Командный центр
            zoom: 13,
            controls: ['zoomControl', 'fullscreenControl'],
          },
          { suppressMapOpenBlock: true }
        )

        // Клик по пустому месту сбрасывает фокус и закрывает карточки
        yandexMapInstance.events.add('click', () => {
          if (focusedRoute.value || selectedStop.value) {
            resetFilter()
          }
        })

        resolve()
      } catch (err) {
        reject(err)
      }
    })
  })
}

// --- ЗАГРУЗКА И АГРЕГАЦИЯ ДАННЫХ ---
const fetchAndAggregateRoutes = async () => {
  try {
    isLoading.value = true
    errorMessage.value = null

    // 1. Получаем список путей
    const summaryRes = await apiClient.get('/path/active')
    const rawSummaries = Array.isArray(summaryRes.data) ? summaryRes.data : []
    if (!rawSummaries.length) return

    // 2. Получаем геометрию каждого пути
    const detailedPromises = rawSummaries.map(async (item: any) => {
      const pathId = item.pathId || item.PathId || item.id || item.Id || item.pathNumber || item.PathNumber
      if (!pathId) return null

      try {
        const detailRes = await apiClient.get(`/path/active/${encodeURIComponent(pathId)}`)
        const data = detailRes.data

        let rawLine = data.routeLine || data.RouteLine || []
        if (typeof rawLine === 'string') {
          try { rawLine = JSON.parse(rawLine) } catch { rawLine = [] }
        }

        return {
          pathId: data.pathId || data.PathId || pathId,
          transportType: data.transportType || data.TransportType || 'bus',
          routeLine: rawLine,
          schedule: (data.schedule || data.Schedule || []).map((s: any) => ({
            stopName: s.stopName || s.StopName || 'Остановка',
            lat: Number(s.lat ?? s.Lat),
            lon: Number(s.lon ?? s.Lon),
            arrivalTime: s.arrivalTime || s.ArrivalTime,
          })),
        } as DetailedRoute
      } catch (err) {
        console.error(`Ошибка загрузки деталей маршрута ${pathId}:`, err)
        return null
      }
    })

    const loadedRoutes = (await Promise.all(detailedPromises)).filter(Boolean) as DetailedRoute[]
    allRoutes.value = loadedRoutes

    // 3. Дедупликация и агрегация остановок по координатам и названию
    const stopsMap = new Map<string, UnifiedStop>()

    loadedRoutes.forEach((route) => {
      route.schedule.forEach((stop) => {
        if (!stop.lat || !stop.lon) return

        // Ключ группировки по координатам до ~10 метров
        const stopKey = `${stop.lat.toFixed(4)}_${stop.lon.toFixed(4)}`

        if (!stopsMap.has(stopKey)) {
          stopsMap.set(stopKey, {
            key: stopKey,
            name: stop.stopName,
            lat: stop.lat,
            lon: stop.lon,
            routes: [],
          })
        }

        const unified = stopsMap.get(stopKey)!
        // Проверяем, не добавлен ли уже этот маршрут в эту остановку
        const alreadyAdded = unified.routes.some((r) => r.pathId === route.pathId)
        if (!alreadyAdded) {
          unified.routes.push({
            pathId: route.pathId,
            transportType: route.transportType,
            arrivalTime: stop.arrivalTime,
          })
        }
      })
    })

    unifiedStops.value = Array.from(stopsMap.values())

    // 4. Отрисовка дефолтного состояния (только метки всех уникальных остановок)
    renderDefaultStops()
  } catch (err: any) {
    console.error('[ОРБИТА-ГИС] Сбой загрузки:', err)
    errorMessage.value = err.message || 'Ошибка загрузки маршрутной сети'
  } finally {
    isLoading.value = false
  }
}

// --- ОТРИСОВКА СОСТОЯНИЯ 1: ВСЕ ОСТАНОВКИ БЕЗ ЛИНИЙ ---
const renderDefaultStops = () => {
  if (!yandexMapInstance) return

  // Очищаем существующие маркеры и линии
  clearMapObjects()

  unifiedStops.value.forEach((stop) => {
    const placemark = new ymaps.Placemark(
      [stop.lat, stop.lon],
      {
        hintContent: `${stop.name} (${stop.routes.length} маршр.)`,
      },
      {
        preset: 'islands#circleDotIcon',
        iconColor: '#00E676', // Неоновый зеленый по умолчанию
        hasBalloon: false,    // Отключаем встроенный балун Яндекса!
      }
    )

    // Клик по остановке открывает Vuetify-карточку
    placemark.events.add('click', (e: any) => {
      e.stopPropagation()
      selectedStop.value = stop
    })

    stopPlacemarkMap.set(stop.key, placemark)
    yandexMapInstance.geoObjects.add(placemark)
  })

  // Центрируем по всем остановкам
  const bounds = yandexMapInstance.geoObjects.getBounds()
  if (bounds && Array.isArray(bounds) && bounds[0] && bounds[1]) {
    yandexMapInstance.setBounds(bounds, { checkZoomRange: true, zoomMargin: 60 })
  }
}

// --- ОТРИСОВКА СОСТОЯНИЯ 2: ФОКУС НА МАРШРУТЕ ---
const focusOnRoute = (pathId: string) => {
  const route = allRoutes.value.find((r) => r.pathId.toLowerCase() === pathId.toLowerCase())
  if (!route || !yandexMapInstance) return

  focusedRoute.value = route

  // 1. Рисуем ТОЛЬКО линию выбранного маршрута
  if (activePolyline) {
    yandexMapInstance.geoObjects.remove(activePolyline)
    activePolyline = null
  }

  if (route.routeLine && route.routeLine.length > 0) {
    activePolyline = new ymaps.Polyline(
      route.routeLine,
      {
        hintContent: `Маршрут ${route.pathId.toUpperCase()}`,
      },
      {
        strokeColor: getRouteColor(route.transportType),
        strokeWidth: 5,
        strokeOpacity: 0.95,
      }
    )
    yandexMapInstance.geoObjects.add(activePolyline)
  }

  // 2. Определяем остановки, входящие в этот маршрут
  const routeStopKeys = new Set(
    route.schedule.map((s) => `${s.lat.toFixed(4)}_${s.lon.toFixed(4)}`)
  )

  // 3. Скрываем лишние остановки, подсвечиваем нужные
  stopPlacemarkMap.forEach((placemark, key) => {
    if (routeStopKeys.has(key)) {
      placemark.options.set('visible', true)
      placemark.options.set('iconColor', getRouteColor(route.transportType))
    } else {
      placemark.options.set('visible', false)
    }
  })

  // 4. Плавно центрируем камеру на линии маршрута
  if (activePolyline && activePolyline.geometry) {
    const bounds = activePolyline.geometry.getBounds()
    if (bounds) {
      yandexMapInstance.setBounds(bounds, {
        checkZoomRange: true,
        zoomMargin: 70,
        duration: 400,
      })
    }
  }
}

// --- СБРОС ФИЛЬТРА (ВОЗВРАТ В ДЕФОЛТ) ---
const resetFilter = () => {
  focusedRoute.value = null
  selectedStop.value = null

  if (activePolyline && yandexMapInstance) {
    yandexMapInstance.geoObjects.remove(activePolyline)
    activePolyline = null
  }

  // Делаем видимыми все остановки и возвращаем неоновый цвет
  stopPlacemarkMap.forEach((placemark) => {
    placemark.options.set('visible', true)
    placemark.options.set('iconColor', '#00E676')
  })
}

const clearMapObjects = () => {
  if (!yandexMapInstance) return
  if (activePolyline) {
    yandexMapInstance.geoObjects.remove(activePolyline)
    activePolyline = null
  }
  stopPlacemarkMap.forEach((placemark) => {
    yandexMapInstance.geoObjects.remove(placemark)
  })
  stopPlacemarkMap.clear()
}

onMounted(async () => {
  try {
    await initMap()
    await fetchAndAggregateRoutes()
  } catch (err: any) {
    errorMessage.value = err.message
    isLoading.value = false
  }
})

onUnmounted(() => {
  if (yandexMapInstance) {
    yandexMapInstance.destroy()
    yandexMapInstance = null
  }
})
</script>

<template>
  <div class="map-viewport">
    <!-- Яндекс.Карта -->
    <div ref="mapContainer" class="yandex-map-canvas" />

    <!-- 1. ПЛАВАЮЩАЯ ПАНЕЛЬ СЕТИ МАРШРУТОВ (СЛЕВА) -->
    <div class="floating-routes-sidebar" :class="{ 'sidebar-collapsed': !isSidebarOpen }">
      <div class="sidebar-header d-flex align-center justify-space-between pa-3">
        <div class="d-flex align-center" v-if="isSidebarOpen">
          <v-icon icon="mdi-routes" color="#00E676" size="20" class="mr-2" />
          <span class="font-mono text-subtitle-2 font-weight-bold text-white tracking-wider">
            МАРШРУТЫ ({{ allRoutes.length }})
          </span>
        </div>
        <v-btn
          :icon="isSidebarOpen ? 'mdi-chevron-left' : 'mdi-chevron-right'"
          variant="text"
          size="x-small"
          rounded="0"@click="isSidebarOpen = !isSidebarOpen"
        />
      </div>

      <div v-if="isSidebarOpen" class="sidebar-body pa-2">
        <v-text-field
          v-model="routeSearch"
          prepend-inner-icon="mdi-magnify"
          placeholder="Поиск по номеру..."
          density="compact"
          variant="outlined"
          color="#00E676"
          hide-details
          class="font-mono mb-2 search-input"
        />

        <div class="routes-scroll-area">
          <div
            v-for="route in filteredRoutes"
            :key="route.pathId"
            class="route-item-card pa-2 mb-1 d-flex align-center justify-space-between"
            :class="{ 'route-item-focused': focusedRoute?.pathId === route.pathId }"
            @click="focusOnRoute(route.pathId)"
          >
            <div class="d-flex align-center">
              <span
                class="route-badge-color mr-2"
                :style="{ backgroundColor: getRouteColor(route.transportType) }"
              />
              <v-icon :icon="getTransportIcon(route.transportType)" size="16" class="mr-2 text-grey-lighten-1" />
              <span class="font-mono font-weight-bold text-body-2 text-white">
                № {{ route.pathId.toUpperCase() }}
              </span>
            </div>

            <span class="text-caption font-mono text-disabled">
              {{ route.schedule.length }} ост.
            </span>
          </div>

          <div v-if="filteredRoutes.length === 0" class="text-caption font-mono text-disabled text-center py-4">
            Маршрут не найден
          </div>
        </div>
      </div>
    </div>

    <!-- 2. ПЛАВАЮЩАЯ КНОПКА СБРОСА ФИЛЬТРА (ПО ЦЕНТРУ СВЕРХУ) -->
    <v-fade-transition>
      <div v-if="focusedRoute" class="floating-reset-container">
        <v-btn
          color="#FF1744"
          variant="flat"
          rounded="0"
          size="small"
          prepend-icon="mdi-close"
          class="font-mono font-weight-bold text-white reset-btn px-4"
          @click="resetFilter"
        >
          СБРОСИТЬ ФИЛЬТР (ВСЕ ОСТАНОВКИ)
        </v-btn>
      </div>
    </v-fade-transition>

    <!-- 3. ДИСПЕТЧЕРСКАЯ КАРТОЧКА ВЫБРАННОЙ ОСТАНОВКИ (СПРАВА ВНИЗУ) -->
    <v-slide-y-reverse-transition>
      <v-card v-if="selectedStop" class="stop-floating-card pa-4" rounded="0" elevation="12">
        <!-- Шапка карточки -->
        <div class="d-flex align-start justify-space-between mb-2">
          <div>
            <div class="text-caption font-mono text-disabled d-flex align-center">
              <v-icon icon="mdi-map-marker-radius" size="14" color="#00E676" class="mr-1" />
              ОСТАНОВОЧНЫЙ ПУНКТ
            </div>
            <div class="text-subtitle-1 font-weight-black font-mono text-white line-clamp-1">
              {{ selectedStop.name }}
            </div>
          </div>
          <v-btn icon="mdi-close" variant="text" size="x-small" rounded="0" @click="selectedStop = null" />
        </div>

        <v-divider class="border-opacity-20 mb-3" />

        <!-- Список проходящих маршрутов -->
        <div class="text-caption font-mono text-grey-lighten-1 mb-2">
          ПРОХОДЯЩИЕ МАРШРУТЫ И ПРИБЫТИЕ ({{ selectedStop.routes.length }}):
        </div>

        <div class="stop-routes-list">
          <div
            v-for="r in sortedStopRoutes"
            :key="r.pathId"
            class="stop-route-row pa-2 mb-1 d-flex align-center justify-space-between"
            :class="{ 'stop-route-active': focusedRoute?.pathId === r.pathId }"
            @click="focusOnRoute(r.pathId)"
          >
            <div class="d-flex align-center">
              <v-chip
                size="small"
                label
                class="font-mono font-weight-bold mr-2 route-chip"
                :style="{
                  borderColor: getRouteColor(r.transportType),
                  color: '#FFFFFF',
                  backgroundColor: 'rgba(255, 255, 255, 0.05)',
                }"variant="outlined"
              >
                <v-icon :icon="getTransportIcon(r.transportType)" size="14" class="mr-1" />
                {{ r.pathId.toUpperCase() }}
              </v-chip>
            </div>

            <div class="text-right">
              <span
                class="text-caption font-mono font-weight-medium"
                :class="formatArrivalTime(r.arrivalTime).isUpcoming ? 'text-green-accent-3' : 'text-grey-lighten-1'"
              >
                {{ formatArrivalTime(r.arrivalTime).label }}
              </span>
            </div>
          </div>
        </div>

        <div class="text-caption font-mono text-disabled text-center mt-3" style="font-size: 10px !important;">
          Кликните по маршруту для перехода в режим слежения
        </div>
      </v-card>
    </v-slide-y-reverse-transition>

    <!-- СТАТУС ЗАГРУЗКИ КАРТЫ -->
    <v-fade-transition>
      <div v-if="isLoading" class="overlay-telemetry-status">
        <v-progress-circular indeterminate color="#00E676" size="20" width="2" class="mr-3" />
        <span class="text-caption font-mono text-grey-lighten-2">СИНХРОНИЗАЦИЯ СЕРВЕРНОЙ КАРТЫ...</span>
      </div>
    </v-fade-transition>

    <!-- АЛЕРТ ОШИБКИ -->
    <v-fade-transition>
      <v-alert
        v-if="errorMessage"
        type="error"
        variant="tonal"
        density="compact"
        class="overlay-alert"
        rounded="0"
        closable
      >
        <span class="font-mono text-caption">{{ errorMessage }}</span>
      </v-alert>
    </v-fade-transition>
  </div>
</template>

<style scoped>
.map-viewport {
  position: relative;
  width: 100%;
  height: 100%;
  background-color: #0A0C10;
  overflow: hidden;
}

.yandex-map-canvas {
  width: 100%;
  height: 100%;
}

/* Строгий темный фильтр для Яндекс.Карт */
:deep(.ymaps-2-1-79-map),
:deep(.ymaps-2-1-79-islets_map) {
  filter: invert(90%) hue-rotate(180%) contrast(90%) brightness(95%);
}

/* 1. БОКОВАЯ ПАНЕЛЬ МАРШРУТОВ */
.floating-routes-sidebar {
  position: absolute;
  top: 16px;
  left: 16px;
  width: 280px;
  max-height: calc(100% - 32px);
  background-color: rgba(22, 27, 34, 0.94);
  border: 1px solid #30363D;
  backdrop-filter: blur(8px);
  z-index: 100;
  display: flex;
  flex-direction: column;
  transition: width 0.25s ease;
}

.sidebar-collapsed {
  width: 48px;
}

.sidebar-header {
  background-color: #0E1116;
  border-bottom: 1px solid #21262D;
}

.search-input {
  background-color: #0E1116;
}

.routes-scroll-area {
  overflow-y: auto;
  max-height: 480px;
}

.route-item-card {
  background-color: rgba(255, 255, 255, 0.02);
  border: 1px solid #21262D;
  cursor: pointer;
  transition: all 0.2s ease;
}

.route-item-card:hover {
  background-color: rgba(255, 255, 255, 0.06);
  border-color: #30363D;
}

.route-item-focused {
  background-color: rgba(41, 182, 246, 0.12) !important;
  border-color: #29B6F6 !important;
}

.route-badge-color {
  width: 4px;
  height: 18px;
  border-radius: 1px;
}

/* 2. ПЛАВАЮЩАЯ КНОПКА СБРОСА ФИЛЬТРА */
.floating-reset-container {
  position: absolute;
  top: 16px;
  left: 50%;
  transform: translateX(-50%);
  z-index: 100;
}

.reset-btn {
  box-shadow: 0 0 16px rgba(255, 23, 68, 0.4) !important;
  border: 1px solid #FF1744 !important;
  letter-spacing: 0.05em;
}

/* 3. КАРТОЧКА ВЫБРАННОЙ ОСТАНОВКИ */
.stop-floating-card {
  position: absolute;
  bottom: 24px;
  right: 24px;
  width: 380px;
  max-width: calc(100vw - 48px);
  background-color: rgba(22, 27, 34, 0.96) !important;
  border: 1px solid #30363D !important;
  backdrop-filter: blur(8px);
  z-index: 101;
  box-shadow: 0 0 24px rgba(0, 0, 0, 0.6) !important;
}

.stop-routes-list {
  max-height: 240px;
  overflow-y: auto;
}

.stop-route-row {
  background-color: rgba(255, 255, 255, 0.02);
  border: 1px solid #21262D;
  cursor: pointer;
  transition: all 0.2s ease;
}

.stop-route-row:hover {
  background-color: rgba(255, 255, 255, 0.07);
  border-color: #00E676;
}

.stop-route-active {
  border-color: #29B6F6 !important;
  background-color: rgba(41, 182, 246, 0.1) !important;
}

.route-chip {
  cursor: pointer;
}

/* Статусы загрузки и ошибок */
.overlay-telemetry-status {
  position: absolute;
  top: 16px;
  right: 16px;
  z-index: 1000;
  display: flex;
  align-items: center;
  padding: 8px 16px;
  background-color: rgba(22, 27, 34, 0.92);
  border: 1px solid #30363D;
  backdrop-filter: blur(6px);
}

.overlay-alert {
  position: absolute;
  bottom: 24px;
  left: 24px;
  right: 24px;
  z-index: 1000;
  border: 1px solid #FF1744 !important;
  background-color: rgba(26, 16, 19, 0.95) !important;
}

.font-mono {
  font-family: 'JetBrains Mono', Consolas, monospace;
}
</style>