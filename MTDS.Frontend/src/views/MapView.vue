<script setup lang="ts">
import { onMounted, onUnmounted, ref } from 'vue'
import apiClient from '../api'

declare const ymaps: any

const mapContainer = ref<HTMLDivElement | null>(null)
const isLoading = ref<boolean>(true)
const errorMessage = ref<string | null>(null)

let yandexMapInstance: any = null

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
            center: [55.4311, 37.5458], // Координаты центра г. Подольск
            zoom: 13,
            controls: ['zoomControl', 'fullscreenControl'],
          },
          { suppressMapOpenBlock: true }
        )
        resolve()
      } catch (err) {
        reject(err)
      }
    })
  })
}

const fetchAndRenderActiveRoutes = async () => {
  try {
    isLoading.value = true
    errorMessage.value = null

    // Шаг 1: Запрашиваем активные маршруты
    const summaryResponse = await apiClient.get('/path/active')
    console.log('[ОРБИТА-ГИС] 1. Ответ /path/active:', summaryResponse.data)

    const rawSummaries = Array.isArray(summaryResponse.data) ? summaryResponse.data : []

    if (!rawSummaries.length) {
      console.warn('[ОРБИТА-ГИС] Список путей пуст!')
      return
    }

    // Шаг 2: Получаем детальную геометрию каждого пути
    const detailedPromises = rawSummaries.map(async (item: any) => {
      const pathId = item.pathId || item.PathId || item.id || item.Id || item.pathNumber || item.PathNumber

      if (!pathId) return null

      try {
        console.log(`[ОРБИТА-ГИС] 2. Запрос геометрии: /path/active/${encodeURIComponent(pathId)}`)
        const detailRes = await apiClient.get(`/path/active/${encodeURIComponent(pathId)}`)
        console.log(`[ОРБИТА-ГИС] 3. Успешно получены детали для ${pathId}:`, detailRes.data)
        return detailRes.data
      } catch (err: any) {
        console.error(`[ОРБИТА-ГИС] Ошибка получения геометрии пути "${pathId}":`, err)
        return null
      }
    })

    const detailedRoutes = (await Promise.all(detailedPromises)).filter(Boolean)

    if (!yandexMapInstance) return

    let firstPoint: [number, number] | null = null

    // Шаг 3: Отрисовка всех объектов прямо на карту
    detailedRoutes.forEach((route: any) => {
      const pathId = route.pathId || route.PathId || '—'
      const transportType = route.transportType || route.TransportType || 'Автобус'
      
      let rawLine = route.routeLine || route.RouteLine || []
      if (typeof rawLine === 'string') {
        try { rawLine = JSON.parse(rawLine) } catch { rawLine = [] }
      }

      const schedule = route.schedule || route.Schedule || []

      console.log(`[ОРБИТА-ГИС] Отрисовка маршрута ${pathId}: точек линии = ${rawLine.length}, остановок = ${schedule.length}`)

      // 3.1. Рисуем нить маршрута (Polyline)
      if (Array.isArray(rawLine) && rawLine.length > 0) {
        if (!firstPoint && rawLine[0] && rawLine[0].length === 2) {
          firstPoint = [rawLine[0][0], rawLine[0][1]]
        }

        const polyline = new ymaps.Polyline(
          rawLine,
          {
            hintContent: `Маршрут ${pathId.toString().toUpperCase()} (${transportType})`,
          },
          {
            strokeColor: '#00B0FF', // Ярко-синий цвет линии
            strokeWidth: 5,
            strokeOpacity: 0.9,
          }
        )
        yandexMapInstance.geoObjects.add(polyline)
      }

      // 3.2. Рисуем маркеры остановок (Placemark)
      if (Array.isArray(schedule) && schedule.length > 0) {
        schedule.forEach((stop: any) => {
          const lat = stop.lat ?? stop.Lat
          const lon = stop.lon ?? stop.Lon
          const stopName = stop.stopName || stop.StopName || 'Остановка'
          const arrivalTime = stop.arrivalTime || stop.ArrivalTime

          if(lat && lon) {
            const placemark = new ymaps.Placemark(
              [Number(lat), Number(lon)],
              {
                hintContent: stopName,
                balloonContent: `
                  <div style="color: #111; font-family: monospace; font-size: 13px; line-height: 1.4;">
                    <strong>Остановка:</strong> ${stopName}

                    <strong>Маршрут:</strong> ${pathId.toString().toUpperCase()}

                    ${arrivalTime ? `<strong>Прибытие:</strong> ${new Date(arrivalTime).toLocaleTimeString()}` : ''}
                  </div>
                `,
              },
              {
                preset: 'islands#circleDotIcon',
                iconColor: '#00E676', // Зеленый неоновый маркер
              }
            )
            yandexMapInstance.geoObjects.add(placemark)
          }
        })
      }
    })

    // Шаг 4: Безопасное масштабирование камеры
    const bounds = yandexMapInstance.geoObjects.getBounds()

    if (bounds && Array.isArray(bounds) && bounds[0] && bounds[1]) {
      // Если границы успешно рассчитаны Яндекс.Картами
      yandexMapInstance.setBounds(bounds, {
        checkZoomRange: true,
        zoomMargin: 60,
      })
    } else if (firstPoint) {
      // Фоллбек: центрируем на первую известную координату маршрута
      yandexMapInstance.setCenter(firstPoint, 13)
    }
  } catch (error: any) {
    console.error('[ОРБИТА-ГИС] Критическая ошибка отрисовки:', error)
    errorMessage.value = error.message || 'Ошибка обработки данных карты'
  } finally {
    isLoading.value = false
  }
}

onMounted(async () => {
  try {
    await initMap()
    await fetchAndRenderActiveRoutes()
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
    <div ref="mapContainer" class="yandex-map-canvas" />

    <v-fade-transition>
      <div v-if="isLoading" class="overlay-telemetry-status">
        <v-progress-circular indeterminate color="#00E676" size="20" width="2" class="mr-3" />
        <span class="text-caption font-mono text-grey-lighten-2">СИНХРОНИЗАЦИЯ СЕРВЕРНОЙ КАРТЫ...</span>
      </div>
    </v-fade-transition>

    <v-fade-transition>
      <v-alert v-if="errorMessage" type="error" variant="tonal" density="compact" class="overlay-alert" rounded="0" closable>
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
}
.yandex-map-canvas {
  width: 100%;
  height: 100%;
}
:deep(.ymaps-2-1-79-map),
:deep(.ymaps-2-1-79-islets_map) {
  filter: invert(90%) hue-rotate(180%) contrast(90%) brightness(95%);
}
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