<script setup lang="ts">
import { onMounted, onUnmounted, ref } from 'vue'
import apiClient from '../api'

// Декларация глобального объекта Яндекс.Карт для TypeScript
declare const ymaps: any

// Интерфейсы данных телеметрии маршрута
interface StopSchedule {
  stopName: string
  lat: number
  lon: number
}

interface ActiveRoute {
  transportType: string
  pathId: string
  routeLine: [number, number][]
  schedule: StopSchedule[]
}

const mapContainer = ref<HTMLDivElement | null>(null)
const isLoading = ref<boolean>(true)
const errorMessage = ref<string | null>(null)

let yandexMapInstance: any = null

// Инициализация экземпляра карты
const initMap = (): Promise<void> => {
  return new Promise((resolve, reject) => {
    if (typeof ymaps === 'undefined') {
      return reject(new Error('Скрипт Яндекс.Карт не загружен. Проверьте тег <script> в index.html'))
    }

    ymaps.ready(() => {
      try {
        yandexMapInstance = new ymaps.Map(
          mapContainer.value,
          {
            center: [55.4311, 37.5458], // Начальные координаты
            zoom: 12,
            controls: ['zoomControl', 'fullscreenControl']
          },
          {
            suppressMapOpenBlock: true // Скрываем рекламу и баннеры Яндекса
          }
        )
        resolve()
      } catch (err) {
        reject(err)
      }
    })
  })
}

// Загрузка активных маршрутов с ASP.NET Core и их отрисовка
const fetchAndRenderActiveRoutes = async () => {
  try {
    isLoading.value = true
    errorMessage.value = null

    // Запрос через настроенный экземпляр Axios (токен подставится автоматически)
    const response = await apiClient.get('/path/active')
    
    console.log('[ОРБИТА] Данные телеметрии с сервера:', response.data)

    // Защищенный парсинг ответа: обрабатываем массивы, обертки и одиночные объекты
    let routes: ActiveRoute[] = []

    if (Array.isArray(response.data)) {
      routes = response.data
    } else if (response.data && Array.isArray(response.data.data)) {
      routes = response.data.data
    } else if (response.data && Array.isArray(response.data.items)) {
      routes = response.data.items
    } else if (response.data && Array.isArray(response.data.value)) {
      routes = response.data.value
    } else if (response.data && typeof response.data === 'object') {
      routes = [response.data as ActiveRoute]
    } else {
      throw new Error('Некорректный формат данных: сервер прислал пустой или нераспознанный ответ.')
    }

    if (!yandexMapInstance) return

    // Коллекция для расчета видимой зоны всех полученных точек
    const boundsCollection = new ymaps.GeoObjectCollection()

    routes.forEach((route) => {
      // 1. Отрисовка траектории маршрута (Polyline)
      if (route.routeLine && route.routeLine.length > 0) {
        const polyline = new ymaps.Polyline(
          route.routeLine,
          {
            hintContent: `Маршрут: ${route.pathId || '—'} (${route.transportType || 'Автобус'})`
          },
          {
            strokeColor: '#00B0FF', // Ярко-синий диспетчерский цвет
            strokeWidth: 4,
            strokeOpacity: 0.9
          }
        )

        yandexMapInstance.geoObjects.add(polyline)
        boundsCollection.add(polyline)
      }

      // 2. Отрисовка остановочных пунктов (Placemark)
      if (route.schedule && route.schedule.length > 0) {
        route.schedule.forEach((stop) => {
          if (stop.lat && stop.lon) {
            const placemark = new ymaps.Placemark(
              [stop.lat, stop.lon],
              {
                hintContent: stop.stopName,
                balloonContent: `
                  <div style="color: #111; font-family: monospace; font-size: 13px; line-height: 1.4;">
                    <strong>Остановка:</strong> ${stop.stopName}

                    <strong>Маршрут:</strong> ${route.pathId || '—'}
                  </div>
                `
              },
              {
                preset: 'islands#circleDotIcon',iconColor: '#00E676' // Неоново-зеленый маркер
              }
            )

            yandexMapInstance.geoObjects.add(placemark)
            boundsCollection.add(placemark)
          }
        })
      }
    })

    // Автоматическая подгонка камеры под отрисованные объекты
    if (boundsCollection.getLength() > 0) {
      yandexMapInstance.setBounds(boundsCollection.getBounds(), {
        checkZoomRange: true,
        zoomMargin: 50
      })
    }
  } catch (error: any) {
    console.error('[ОРБИТА] Ошибка загрузки маршрутов:', error)
    errorMessage.value = error.message || 'Сбой получения телеметрии активных путей'
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

// Очистка ресурсов карты при переходе на другой маршрут роутера
onUnmounted(() => {
  if (yandexMapInstance) {
    yandexMapInstance.destroy()
    yandexMapInstance = null
  }
})
</script>

<template>
  <div class="map-viewport">
    <!-- Контейнер для рендеринга Яндекс.Карты -->
    <div ref="mapContainer" class="yandex-map-canvas" />

    <!-- Панель статуса опроса сервера -->
    <v-fade-transition>
      <div v-if="isLoading" class="overlay-telemetry-status">
        <v-progress-circular indeterminate color="#00E676" size="20" width="2" class="mr-3" />
        <span class="text-caption font-mono text-grey-lighten-2">СИНХРОНИЗАЦИЯ ТЕЛЕМЕТРИИ...</span>
      </div>
    </v-fade-transition>

    <!-- Плашка ошибки (если упал бэкенд или сеть) -->
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
  overflow: hidden;
  background-color: #0A0C10;
}

.yandex-map-canvas {
  width: 100%;
  height: 100%;
}

/* 
  Стилизация тайлов под Dark Mode командного центра.
  Инвертирует светлую стандартную карту в тактический темный режим.
*/
:deep(.ymaps-2-1-79-map),
:deep(.ymaps-2-1-79-islets_map) {
  filter: invert(90%) hue-rotate(180%) contrast(90%) brightness(95%);
}

/* Плашка загрузки */
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

/* Плашка ошибки */
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
  font-family: "JetBrains Mono", Consolas, monospace;
}
</style>