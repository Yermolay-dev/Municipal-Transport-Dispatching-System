<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'

const router = useRouter()
const drawer = ref<boolean>(true)

const dispatcher = ref({
  name: 'Алексеев Д. В.',
  role: localStorage.getItem('role') || 'Диспетчер',
  badge: 'Смена №2',
  initials: 'ДА',
})

const menuItems = [
  { to: '/', title: 'Карта маршрутов', icon: 'mdi-map-marker-path' },
  { to: '/fleet', title: 'Управление парком', icon: 'mdi-bus-multiple' },
  { to: '/audit', title: 'Журнал аудита', icon: 'mdi-clipboard-text-clock-outline' },
]

const handleLogout = () => {
  localStorage.removeItem('token')
  localStorage.removeItem('role')
  router.push('/login')
}
</script>

<template>
  <div class="h-100 w-100">
    <!-- БОКОВОЕ МЕНЮ -->
    <v-navigation-drawer
      v-model="drawer"
      app
      permanent
      width="260"
      class="panel-drawer"
    >
      <div class="brand-header px-4 py-3 d-flex align-center">
        <v-icon icon="mdi-radar" color="#00E676" size="24" class="mr-2" />
        <div class="d-flex flex-column">
          <span class="text-subtitle-1 font-weight-black tracking-wider brand-title">ОРБИТА</span>
          <span class="text-caption text-disabled tracking-widest" style="font-size: 9px !important; margin-top: -3px;">
            СДМТ • СИСТЕМА ДИСПЕТЧЕРИЗАЦИИ
          </span>
        </div>
      </div>

      <v-divider class="border-opacity-10 mb-2" />

      <v-list density="compact" nav class="px-2">
        <v-list-item
          v-for="item in menuItems"
          :key="item.to"
          :to="item.to"
          exact
          active-class="menu-item-active"
          rounded="0"
          class="mb-1 menu-item"
        >
          <template #prepend>
            <v-icon :icon="item.icon" size="20" class="mr-2" />
          </template>
          <v-list-item-title class="text-body-2 font-weight-medium">
            {{ item.title }}
          </v-list-item-title>
        </v-list-item>
      </v-list>

      <template #append>
        <div class="px-3 py-2 system-health-box mx-2 mb-2 d-flex align-center justify-space-between">
          <span class="text-caption text-disabled">ТЕЛЕМЕТРИЯ:</span>
          <span class="text-caption font-weight-bold status-normal">НОРМА</span>
        </div>
      </template>
    </v-navigation-drawer>

    <!-- ВЕРХНЯЯ ПАНЕЛЬ -->
    <v-app-bar density="compact" flat class="panel-appbar px-2">
      <v-btn icon="mdi-menu" variant="text" size="small" rounded="0" @click="drawer = !drawer" />

      <div class="d-flex align-center ml-3">
        <span class="pulse-indicator mr-2" />
        <span class="text-caption font-weight-bold letter-spacing-1 text-grey-lighten-1">
          КОНТУР МОНИТОРИНГА АКТИВЕН
        </span>
      </div>

      <v-spacer />

      <v-btn icon variant="text" size="small" rounded="0" class="mr-2">
        <v-badge color="#FF1744" content="2" offset-x="2" offset-y="2">
          <v-icon icon="mdi-bell-outline" size="20" />
        </v-badge>
      </v-btn>

      <v-divider vertical class="mx-2 my-2 border-opacity-10" />

      <v-menu location="bottom end" transition="fade-transition">
        <template #activator="{ props }">
          <v-btn v-bind="props" variant="text" class="dispatcher-btn px-2" rounded="0">
            <v-avatar size="28" color="#21262D" class="mr-2 avatar-border">
              <span class="text-caption font-weight-bold">{{ dispatcher.initials }}</span>
            </v-avatar>
            <div class="text-left mr-1 d-none d-sm-flex flex-column">
              <span class="text-caption font-weight-bold line-clamp-1">{{ dispatcher.name }}</span>
              <span class="text-disabled" style="font-size: 10px;">{{ dispatcher.badge }}</span>
            </div>
            <v-icon icon="mdi-chevron-down" size="16" class="text-disabled" />
          </v-btn>
        </template>

        <v-list-item density="compact" class="dispatcher-menu py-0 mt-1" min-width="180">
          <v-list-item class="py-2"><v-list-item-title class="text-body-2 font-weight-bold">{{ dispatcher.name }}</v-list-item-title>
            <v-list-item-subtitle class="text-caption">{{ dispatcher.role }}</v-list-item-subtitle>
          </v-list-item>
          <v-divider class="border-opacity-10" />
          <v-list-item prepend-icon="mdi-account-circle-outline" title="Профиль" rounded="0" />
          <v-divider class="border-opacity-10" />
          <v-list-item
            prepend-icon="mdi-logout-variant"
            title="Завершить смену"
            rounded="0"
            class="text-red-lighten-1"
            @click="handleLogout"
          />
        </v-list-item>
      </v-menu>
    </v-app-bar>

    <!-- ВЛОЖЕННЫЕ СТРАНИЦЫ (Карта, Парк, Аудит) -->
    <v-main class="work-viewport">
      <router-view />
    </v-main>
  </div>
</template>

<style scoped>
.panel-drawer {
  background-color: #161B22 !important;
  border-right: 1px solid #21262D !important;
}
.panel-appbar {
  background-color: #161B22 !important;
  border-bottom: 1px solid #21262D !important;
}
.brand-title {
  letter-spacing: 0.12em;
  color: #F0F6FC;
}
.menu-item {
  border-left: 2px solid transparent;
  transition: all 0.15s ease;
}
.menu-item:hover {
  background-color: rgba(255, 255, 255, 0.04) !important;
}
.menu-item-active {
  background-color: rgba(0, 230, 118, 0.08) !important;
  border-left: 2px solid #00E676 !important;
  color: #00E676 !important;
}
.avatar-border {
  border: 1px solid #30363D;
}
.dispatcher-btn {
  height: 38px !important;
}
.dispatcher-menu {
  background-color: #161B22 !important;
  border: 1px solid #30363D !important;
}
.pulse-indicator {
  width: 8px;
  height: 8px;
  background-color: #00E676;
  border-radius: 50%;
  box-shadow: 0 0 8px #00E676;
}
.status-normal {
  color: #00E676;
  letter-spacing: 0.05em;
}
.system-health-box {
  background-color: #0E1116;
  border: 1px solid #21262D;
  border-radius: 2px;
}
.work-viewport {
  background-color: #0A0C10;
  height: 100vh;
  overflow: hidden;
}
</style>