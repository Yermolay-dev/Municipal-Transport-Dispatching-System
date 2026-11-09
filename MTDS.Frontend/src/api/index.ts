import axios from 'axios'
import router from '../router'

// Создаем инстанс с базовым URL бэкенда
const apiClient = axios.create({
    baseURL: 'http://localhost:5067/api/v1',
    headers: {
        'Content-Type': 'application/json',
    },
})

// Request Interceptor: внедряем Bearer-токен в каждый исходящий запрос
apiClient.interceptors.request.use(
    (config) => {
        const token = localStorage.getItem('token')
        if (token && config.headers) {
            config.headers.Authorization = `Bearer ${token}`
        }
        return config
    },
    (error) => {
        return Promise.reject(error)
    }
)

// Response Interceptor: перехват 401 Unauthorized
apiClient.interceptors.response.use(
    (response) => response,
    (error) => {
        if (error.response && error.response.status === 401) {
            // Сбрасываем сессию
            localStorage.removeItem('token')
            localStorage.removeItem('role')

            // Редирект на экран авторизации, если мы еще не там
            if (router.currentRoute.value.path !== '/login') {
                router.push('/login')
            }
        }
        return Promise.reject(error)
    }
)

export default apiClient