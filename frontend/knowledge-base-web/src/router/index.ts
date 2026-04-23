import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const routes: RouteRecordRaw[] = [
  {
    path: '/login',
    name: 'Login',
    component: () => import('../views/Login.vue'),
    meta: { requiresAuth: false }
  },
  {
    path: '/',
    component: () => import('../layouts/MainLayout.vue'),
    meta: { requiresAuth: true },
    children: [
      { path: '', name: 'Home', component: () => import('../views/Home.vue') },
      { path: 'dashboard', name: 'Dashboard', component: () => import('../views/Dashboard.vue') },
      { path: 'spaces', name: 'Spaces', component: () => import('../views/Spaces.vue') },
      { path: 'spaces/:id', name: 'SpaceDetail', component: () => import('../views/SpaceDetail.vue') },
      { path: 'documents/:id', name: 'DocumentDetail', component: () => import('../views/DocumentDetail.vue') },
      { path: 'documents/:id/edit', name: 'DocumentEdit', component: () => import('../views/DocumentEdit.vue') },
      { path: 'documents/new', name: 'DocumentNew', component: () => import('../views/DocumentEdit.vue') },
      { path: 'learning', name: 'Learning', component: () => import('../views/Learning.vue') },
      { path: 'exams', name: 'Exams', component: () => import('../views/Exams.vue') },
      { path: 'exams/:id', name: 'ExamDetail', component: () => import('../views/ExamDetail.vue') },
      { path: 'admin/users', name: 'AdminUsers', component: () => import('../views/admin/Users.vue'), meta: { roles: ['SuperAdmin'] } },
      { path: 'admin/departments', name: 'AdminDepartments', component: () => import('../views/admin/Departments.vue'), meta: { roles: ['SuperAdmin'] } },
      { path: 'admin/system', name: 'AdminSystem', component: () => import('../views/admin/System.vue'), meta: { roles: ['SuperAdmin'] } },
      { path: 'admin/logs', name: 'AdminLogs', component: () => import('../views/admin/Logs.vue'), meta: { roles: ['SuperAdmin'] } },
    ]
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

router.beforeEach((to, _from, next) => {
  const auth = useAuthStore()
  if (to.meta.requiresAuth !== false && !auth.isLoggedIn) {
    next('/login')
  } else if (to.meta.roles && !(to.meta.roles as string[]).includes(auth.role)) {
    next('/')
  } else {
    next()
  }
})

export default router
