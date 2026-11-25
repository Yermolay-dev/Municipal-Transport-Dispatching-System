import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'
import MainLayout from '../layouts/MainLayout.vue'
import MapView from '../views/MapView.vue'
import LoginView from '../views/LoginView.vue'
import FleetView from '../views/FleetView.vue'
import AuditView from '../views/AuditView.vue'
import RoutesAdminView from '../views/RoutesAdminView.vue'
import StaffAdminView from '../views/StaffAdminView.vue'
import ProfileView from '../views/ProfileView.vue' // <-- Импортируем страницу профиля

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
            {
                path: 'admin/routes',
                name: 'routes-admin',
                component: RoutesAdminView,
                meta: { requiresAdmin: true, title: 'Маршруты и остановки' },
            },
            {
                path: 'admin/staff',
                name: 'staff-admin',
                component: StaffAdminView,
                meta: { requiresAdmin: true, title: 'Персонал' },
            },
            // ПРОФИЛЬ СОТРУДНИКА (Доступно Admin и Dispatcher)
            {
                path: 'profile',
                name: 'profile',
                component: ProfileView,
                meta: { title: 'Профиль сотрудника' },
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

    if (token && role !== 'Admin' && role !== 'Dispatcher') {
        localStorage.removeItem('token')
        localStorage.removeItem('role')
        return '/login'
    }

    if (token && to.path === '/login') {
        return '/'
    }

    if (to.meta.requiresAdmin && role !== 'Admin') {
        return '/'
    }

    return true
})

export default router