import axios from 'axios'
import router from '../router'

const apiClient = axios.create({
    baseURL: 'http://localhost:5067/api/v1',
    headers: {
        'Content-Type': 'application/json',
    },
})

// Request Interceptor: логируем отправляемый заголовок
apiClient.interceptors.request.use((config) => {
    const token = localStorage.getItem('token')
    if (token && config.headers) {
        config.headers.Authorization = `Bearer ${token}`
        console.log(`[HTTP REQ] ${config.method?.toUpperCase()} ${config.url} -> Bearer ${token.substring(0, 10)}...`)
    } else {
        console.warn(`[HTTP REQ] ${config.method?.toUpperCase()} ${config.url} -> БЕЗ ТОКЕНА!`)
    }
    return config
})

// Response Interceptor
apiClient.interceptors.response.use(
    (response) => response,
    (error) => {
        if (error.response && error.response.status === 401) {
            console.warn('[HTTP RES 401] Запрос отклонен сервером (Unauthorized). Сброс сессии.')
            localStorage.removeItem('token')
            localStorage.removeItem('role')
            if (router.currentRoute.value.path !== '/login') {
                router.push('/login')
            }
        }
        return Promise.reject(error)
    }
)

export default apiClient