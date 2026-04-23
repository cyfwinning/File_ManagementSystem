import axios from 'axios'
import { ElMessage } from 'element-plus'
import { useAuthStore } from '../stores/auth'

const http = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5000/api',
  timeout: 30000,
})

// Prevent multiple simultaneous redirects to login
let isRedirecting = false

http.interceptors.request.use((config) => {
  const auth = useAuthStore()
  if (auth.token) {
    config.headers.Authorization = `Bearer ${auth.token}`
  }
  // Always send the selected database type
  if (auth.databaseType) {
    config.headers['X-Database-Type'] = auth.databaseType
  }
  return config
})

http.interceptors.response.use(
  (response) => response.data,
  (error) => {
    const status = error.response?.status
    const msg = error.response?.data?.message || error.message || '请求失败'

    if (isRedirecting) {
      return Promise.reject(error)
    }

    if (status === 401) {
      // Token expired or invalid - redirect to login
      isRedirecting = true
      const auth = useAuthStore()
      auth.logout()
      ElMessage.error('登录已过期，请重新登录')
      setTimeout(() => {
        window.location.href = '/login'
      }, 300)
    } else if (status === 503 && error.response?.data?.databaseType) {
      // Database connection failure - clear auth and redirect to login
      // so the user can re-select database type and re-login
      isRedirecting = true
      const auth = useAuthStore()
      auth.logout()
      ElMessage.error(msg)
      setTimeout(() => {
        window.location.href = '/login'
      }, 1500)
    } else {
      ElMessage.error(msg)
    }
    return Promise.reject(error)
  }
)

export default http
