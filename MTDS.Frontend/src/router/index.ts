import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'
import MainLayout from '../layouts/MainLayout.vue'
import MapView from '../views/MapView.vue'
import LoginView from '../views/LoginView.vue'
import FleetView from '../views/FleetView.vue'
import AuditView from '../views/AuditView.vue'
import RoutesAdminView from '../views/RoutesAdminView.vue'
import StaffAdminView from '../views/StaffAdminView.vue' // <-- Импортируем раздел персонала

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
            // РАЗДЕЛ УПРАВЛЕНИЯ ПЕРСОНАЛОМ (ТОЛЬКО ADMIN)
            {
                path: 'admin/staff',
                name: 'staff-admin',
                component: StaffAdminView,
                meta: { requiresAdmin: true, title: 'Персонал' },
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

// Навигационный хук с усиленной проверкой ролей
router.beforeEach((to) => {
    const token = localStorage.getItem('token')
    const role = localStorage.getItem('role')
    const isPublic = to.meta.public === true

    // 1. Попытка открыть закрытую страницу без токена
    if (!token && !isPublic) {
        return '/login'
    }

    // 2. УСИЛЕННАЯ ПРОВЕРКА РОЛЕЙ:
    // Если токен есть, но роль не относится к персоналу диспетчерского центра (например, Driver)
    if (token && role !== 'Admin' && role !== 'Dispatcher') {
        localStorage.removeItem('token')
        localStorage.removeItem('role')
        return '/login'
    }

    // 3. Авторизованный пользователь пытается зайти на страницу логина
    if (token && to.path === '/login') {
        return '/'
    }

    // 4. RBAC: Защита админских маршрутов от обычных диспетчеров
    if (to.meta.requiresAdmin && role !== 'Admin') {
        return '/'
    }

    return true
})

export default router