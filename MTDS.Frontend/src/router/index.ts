import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'
import MainLayout from '../layouts/MainLayout.vue'
import MapView from '../views/MapView.vue'
import LoginView from '../views/LoginView.vue'
import FleetView from '../views/FleetView.vue'
import AuditView from '../views/AuditView.vue'

const routes: RouteRecordRaw[] = [
    {
        path: '/login',
        name: 'login',
        component: LoginView,
        meta: { public: true, title: 'Вход в систему' },
    },
    {
        path: '/',
        component: MainLayout,
        children: [
            {
                path: '',
                name: 'map',
                component: MapView,
                meta: { title: 'Карта маршрутов' },
            },
            {
                path: 'fleet',
                name: 'fleet',
                component: FleetView,
                meta: { title: 'Управление парком' },
            },
            {
                path: 'audit',
                name: 'audit',
                component: AuditView,
                meta: { requiresAdmin: true, title: 'Журнал аудита' },
            },
        ],
    },
    {
        path: '/:pathMatch(.*)*',
        redirect: '/',
    },
]

const router = createRouter({
    history: createWebHistory(),
    routes,
})

// Современный навигационный хук без вызова устаревшего callback'а next()
router.beforeEach((to) => {
    const token = localStorage.getItem('token')
    const role = localStorage.getItem('role')
    const isPublic = to.meta.public === true

    // 1. Не авторизован и идет в закрытую зону
    if (!token && !isPublic) {
        return '/login'
    }

    // 2. Авторизован и пытается открыть /login
    if (token && to.path === '/login') {
        return '/'
    }

    // 3. RBAC: Проверка прав администратора
    if (to.meta.requiresAdmin && role !== 'Admin') {
        return '/'
    }

    // Штатный переход разрешен
    return true
})

export default router