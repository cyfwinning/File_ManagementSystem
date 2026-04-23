<template>
  <el-container class="main-layout">
    <el-aside :width="isCollapse ? '64px' : '240px'" class="aside">
      <div class="logo">
        <el-icon :size="28"><Reading /></el-icon>
        <span v-show="!isCollapse" class="logo-text">南芯知识库</span>
      </div>
      <el-menu
        :default-active="route.path"
        :collapse="isCollapse"
        router
        background-color="#1d1e1f"
        text-color="#bfcbd9"
        active-text-color="#409eff"
        class="side-menu"
      >
        <el-menu-item index="/">
          <el-icon><HomeFilled /></el-icon>
          <template #title>首页</template>
        </el-menu-item>
        <el-menu-item index="/dashboard">
          <el-icon><DataAnalysis /></el-icon>
          <template #title>数据驾驶舱</template>
        </el-menu-item>
        <el-menu-item index="/spaces">
          <el-icon><FolderOpened /></el-icon>
          <template #title>知识空间</template>
        </el-menu-item>
        <el-menu-item index="/learning">
          <el-icon><TrophyBase /></el-icon>
          <template #title>学习中心</template>
        </el-menu-item>
        <el-menu-item index="/exams">
          <el-icon><EditPen /></el-icon>
          <template #title>考试中心</template>
        </el-menu-item>
        <el-sub-menu index="/admin" v-if="auth.isAdmin">
          <template #title>
            <el-icon><Setting /></el-icon>
            <span>系统管理</span>
          </template>
          <el-menu-item index="/admin/users">用户管理</el-menu-item>
          <el-menu-item index="/admin/departments">部门管理</el-menu-item>
          <el-menu-item index="/admin/system">系统配置</el-menu-item>
          <el-menu-item index="/admin/logs">操作日志</el-menu-item>
        </el-sub-menu>
      </el-menu>
    </el-aside>

    <el-container>
      <el-header class="header">
        <div class="header-left">
          <el-icon class="collapse-btn" @click="isCollapse = !isCollapse" :size="20">
            <Fold v-if="!isCollapse" />
            <Expand v-else />
          </el-icon>
          <el-input v-model="searchKeyword" placeholder="搜索文档..." class="search-input" clearable @keyup.enter="handleSearch">
            <template #prefix><el-icon><Search /></el-icon></template>
          </el-input>
        </div>
        <div class="header-right">
          <el-dropdown trigger="click">
            <div class="user-info">
              <el-avatar :size="32" :style="{ backgroundColor: '#409eff' }">{{ auth.displayName?.charAt(0) }}</el-avatar>
              <span class="user-name">{{ auth.displayName }}</span>
              <el-icon><ArrowDown /></el-icon>
            </div>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item>
                  <el-tag size="small" type="primary">{{ auth.role }}</el-tag>
                </el-dropdown-item>
                <el-dropdown-item divided @click="handleLogout">退出登录</el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </el-header>

      <el-main class="main-content">
        <router-view />
      </el-main>
    </el-container>
  </el-container>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const isCollapse = ref(false)
const searchKeyword = ref('')

function handleSearch() {
  if (searchKeyword.value) {
    router.push({ name: 'Spaces', query: { keyword: searchKeyword.value } })
  }
}

function handleLogout() {
  auth.logout()
  router.push('/login')
}
</script>

<style scoped>
.main-layout {
  height: 100vh;
}
.aside {
  background-color: #1d1e1f;
  transition: width 0.3s;
  overflow-x: hidden;
}
.logo {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 60px;
  color: #fff;
  gap: 8px;
  border-bottom: 1px solid #333;
}
.logo-text {
  font-size: 18px;
  font-weight: 600;
  white-space: nowrap;
}
.side-menu {
  border-right: none;
  height: calc(100vh - 60px);
  overflow-y: auto;
}
.header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  background: #fff;
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.08);
  padding: 0 20px;
}
.header-left {
  display: flex;
  align-items: center;
  gap: 16px;
}
.collapse-btn {
  cursor: pointer;
  color: #606266;
}
.search-input {
  width: 360px;
}
.header-right {
  display: flex;
  align-items: center;
}
.user-info {
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
}
.user-name {
  font-size: 14px;
  color: #303133;
}
.main-content {
  background: #f5f7fa;
  min-height: calc(100vh - 60px);
}
</style>
