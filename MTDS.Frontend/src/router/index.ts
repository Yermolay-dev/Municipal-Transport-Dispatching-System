import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'
import MainLayout from '../layouts/MainLayout.vue'
import MapView from '../views/MapView.vue'
import LoginView from '../views/LoginView.vue'
import FleetView from '../views/FleetView.vue'
import AuditView from '../views/AuditView.vue'
import RoutesAdminView from '../views/RoutesAdminView.vue' // <-- Импортируем новый View

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
            // 4-Й РАЗДЕЛ (ТОЛЬКО ДЛЯ АДМИНИСТРАТОРА)
            {
                path: 'admin/routes',
                name: 'routes-admin',
                component: RoutesAdminView,
                meta: { requiresAdmin: true, title: 'Маршруты и остановки' },
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

router.beforeEach((to) => {
    const token = localStorage.getItem('token')
    const role = localStorage.getItem('role')
    const isPublic = to.meta.public === true

    if (!token && !isPublic) {
        return '/login'
    }
    if (token && to.path === '/login') {
        return '/'
    }
    // RBAC-проверка: не-Admin не попадет на /audit и /admin/routes
    if (to.meta.requiresAdmin && role !== 'Admin') {
        return '/'
    }

    return true
})

export default router