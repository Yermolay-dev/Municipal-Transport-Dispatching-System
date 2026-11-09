import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'
import { h } from 'vue'
import MainLayout from '../layouts/MainLayout.vue'
import MapView from '../views/MapView.vue'
import LoginView from '../views/LoginView.vue'

// Заглушки для вспомогательных страниц
const AuditView = {
    render: () => h('div', { class: 'pa-6 font-mono text-grey-lighten-1' }, [
        h('h2', { class: 'text-h6 mb-2' }, '[ЖУРНАЛ АУДИТА]'),
        h('p', { class: 'text-caption text-disabled' }, 'Фиксация системных инцидентов и телеметрии водителей.')
    ])
}

const FleetView = {
    render: () => h('div', { class: 'pa-6 font-mono text-grey-lighten-1' }, [
        h('h2', { class: 'text-h6 mb-2' }, '[УПРАВЛЕНИЕ ПАРКОМ]'),
        h('p', { class: 'text-caption text-disabled' }, 'Реестр подвижного состава и сменных графиков.')
    ])
}

const routes: RouteRecordRaw[] = [
    // 1. ПУБЛИЧНЫЙ МАРШРУТ (Без бокового меню и бара)
    {
        path: '/login',
        name: 'login',
        component: LoginView,
        meta: { public: true, title: 'Вход в систему' }
    },

    // 2. ЗАЩИЩЕННЫЙ КОНТУР (Внутри MainLayout)
    {
        path: '/',
        component: MainLayout,
        children: [
            {
                path: '',
                name: 'map',
                component: MapView,
                meta: { title: 'Карта маршрутов' }
            },
            {
                path: 'fleet',
                name: 'fleet',
                component: FleetView,
                meta: { title: 'Управление парком' }
            },
            {
                path: 'audit',
                name: 'audit',
                component: AuditView,
                meta: { title: 'Журнал аудита' }
            }
        ]
    },

    // Редирект для всех несуществующих путей
    {
        path: '/:pathMatch(.*)*',
        redirect: '/'
    }
]

const router = createRouter({
    history: createWebHistory(),
    routes
})

// ГЛОБАЛЬНЫЙ GUARD (Защита маршрутов)
router.beforeEach((to, from, next) => {
    const token = localStorage.getItem('token')
    const isPublic = to.meta.public === true

    if (!token && !isPublic) {
        // 1. Попытка зайти в закрытую зону без токена -> на Login
        next('/login')
    } else if (token && to.path === '/login') {
        // 2. Попытка открыть страницу логина, будучи авторизованным -> на Главную
        next('/')
    } else {
        // 3. Штатный переход
        next()
    }
})

export default router