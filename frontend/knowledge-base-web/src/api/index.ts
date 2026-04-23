import http from './http'

// Auth
export const login = (data: { Username: string; Password: string; DatabaseType?: string }) => http.post('/auth/login', data)
export const getDatabaseOptions = () => http.get('/auth/database-options')

// Users
export const getUsers = (params?: any) => http.get('/users', { params })
export const getUser = (id: number) => http.get(`/users/${id}`)
export const getCurrentUser = () => http.get('/users/me')
export const createUser = (data: any) => http.post('/users', data)
export const updateUser = (id: number, data: any) => http.put(`/users/${id}`, data)
export const deleteUser = (id: number) => http.delete(`/users/${id}`)

// Departments
export const getDepartmentTree = () => http.get('/departments/tree')
export const createDepartment = (data: any) => http.post('/departments', data)
export const updateDepartment = (id: number, data: any) => http.put(`/departments/${id}`, data)
export const deleteDepartment = (id: number) => http.delete(`/departments/${id}`)

// Spaces
export const getSpaces = () => http.get('/spaces')
export const createSpace = (data: any) => http.post('/spaces', data)
export const updateSpace = (id: number, data: any) => http.put(`/spaces/${id}`, data)
export const deleteSpace = (id: number) => http.delete(`/spaces/${id}`)

// Categories
export const getCategoriesBySpace = (spaceId: number) => http.get(`/categories/space/${spaceId}`)
export const createCategory = (data: any) => http.post('/categories', data)
export const updateCategory = (id: number, data: any) => http.put(`/categories/${id}`, data)
export const deleteCategory = (id: number) => http.delete(`/categories/${id}`)

// Documents
export const getDocuments = (params?: any) => http.get('/documents', { params })
export const getDocument = (id: number) => http.get(`/documents/${id}`)
export const createDocument = (data: any) => http.post('/documents', data)
export const updateDocument = (id: number, data: any) => http.put(`/documents/${id}`, data)
export const deleteDocument = (id: number) => http.delete(`/documents/${id}`)
export const togglePin = (id: number) => http.post(`/documents/${id}/pin`)
export const toggleFavorite = (id: number) => http.post(`/documents/${id}/favorite`)
export const getDocumentVersions = (id: number) => http.get(`/documents/${id}/versions`)
export const getDocumentComments = (id: number) => http.get(`/documents/${id}/comments`)
export const addComment = (data: any) => http.post('/documents/comments', data)
export const getAttachments = (id: number) => http.get(`/documents/${id}/attachments`)
export const uploadAttachment = (id: number, formData: FormData) => http.post(`/documents/${id}/attachments`, formData, { headers: { 'Content-Type': 'multipart/form-data' } })
export const deleteAttachment = (id: number) => http.delete(`/documents/attachments/${id}`)
export const getAttachmentPreviewUrl = (id: number) => `/api/documents/attachments/${id}/preview`
export const getAttachmentDownloadUrl = (id: number) => `/api/documents/attachments/${id}/download`
export const getAttachmentPermissions = (id: number) => http.get(`/documents/attachments/${id}/permissions`)
export const setAttachmentPermission = (data: any) => http.post('/documents/attachments/permissions', data)
export const deleteAttachmentPermission = (id: number) => http.delete(`/documents/attachments/permissions/${id}`)

// Recommendations
export const getMyRecommendations = () => http.get('/recommendations/my')
export const createRecommendation = (data: any) => http.post('/recommendations', data)

// Learning
export const getLearningStats = () => http.get('/learning/stats')
export const getLearningRecords = (params?: any) => http.get('/learning/records', { params })
export const updateLearningProgress = (data: any) => http.post('/learning/progress', data)

// Exams
export const getExams = (params?: any) => http.get('/exams', { params })
export const getExamDetail = (id: number) => http.get(`/exams/${id}`)
export const createExam = (data: any) => http.post('/exams', data)
export const submitExam = (data: any, timeSpentSeconds: number) => http.post(`/exams/submit?timeSpentSeconds=${timeSpentSeconds}`, data)

// Dashboard
export const getDashboardPersonal = () => http.get('/dashboard/personal')
export const getDashboardTrend = (period: string) => http.get('/dashboard/trend', { params: { period } })
export const getDashboardDepartment = () => http.get('/dashboard/department')
export const getDashboardUserStats = (userId: number) => http.get(`/dashboard/user/${userId}`)

// System
export const getSystemConfigs = () => http.get('/system/configs')
export const updateSystemConfig = (id: number, value: string) => http.put(`/system/configs/${id}`, JSON.stringify(value), { headers: { 'Content-Type': 'application/json' } })
export const getOperationLogs = (params?: any) => http.get('/system/logs', { params })
export const getDatabaseConfig = () => http.get('/system/database/config')
export const configureDatabaseConfig = (data: any) => http.post('/system/database/config', data)
export const testMySqlConnection = (data?: any) => http.post('/system/database/test-mysql', data || {})
