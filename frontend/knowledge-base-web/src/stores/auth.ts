import { defineStore } from 'pinia'
import { ref, computed } from 'vue'

export const useAuthStore = defineStore('auth', () => {
  const token = ref(localStorage.getItem('kb_token') || '')
  const userId = ref(Number(localStorage.getItem('kb_userId')) || 0)
  const displayName = ref(localStorage.getItem('kb_displayName') || '')
  const role = ref(localStorage.getItem('kb_role') || '')
  const databaseType = ref(localStorage.getItem('kb_databaseType') || '')

  const isLoggedIn = computed(() => !!token.value)
  const isLeader = computed(() => ['SuperAdmin', 'CompanyLeader', 'DepartmentLeader'].includes(role.value))
  const isAdmin = computed(() => role.value === 'SuperAdmin')

  function setAuth(data: any) {
    // Backend returns camelCase JSON fields
    token.value = data.token || data.Token || ''
    displayName.value = data.displayName || data.DisplayName || ''
    role.value = data.role || data.Role || ''
    userId.value = data.userId || data.UserId || 0
    const dbType = data.databaseType || data.DatabaseType
    if (dbType) {
      databaseType.value = dbType
      localStorage.setItem('kb_databaseType', dbType)
    }
    localStorage.setItem('kb_token', token.value)
    localStorage.setItem('kb_displayName', displayName.value)
    localStorage.setItem('kb_role', role.value)
    localStorage.setItem('kb_userId', String(userId.value))
  }

  function setDatabaseType(type: string) {
    databaseType.value = type
    localStorage.setItem('kb_databaseType', type)
  }

  function logout() {
    token.value = ''
    displayName.value = ''
    role.value = ''
    userId.value = 0
    databaseType.value = ''
    localStorage.removeItem('kb_token')
    localStorage.removeItem('kb_displayName')
    localStorage.removeItem('kb_role')
    localStorage.removeItem('kb_userId')
    localStorage.removeItem('kb_databaseType')
  }

  return { token, userId, displayName, role, databaseType, isLoggedIn, isLeader, isAdmin, setAuth, setDatabaseType, logout }
})
