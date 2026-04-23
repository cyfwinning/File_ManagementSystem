<template>
  <div class="login-page">
    <div class="login-card">
      <div class="login-header">
        <el-icon :size="48" color="#409eff"><Reading /></el-icon>
        <h1>企业智能知识库</h1>
        <p class="subtitle">浙江南芯半导体</p>
      </div>
      <el-form :model="form" :rules="rules" ref="formRef" @submit.prevent="handleLogin" class="login-form">
        <el-form-item prop="Username">
          <el-input v-model="form.Username" placeholder="用户名" size="large" prefix-icon="User" />
        </el-form-item>
        <el-form-item prop="Password">
          <el-input v-model="form.Password" placeholder="密码" size="large" prefix-icon="Lock" type="password" show-password />
        </el-form-item>
        <!-- Database selector: only shown when both databases are enabled -->
        <el-form-item v-if="dbOptions.length > 1" class="db-selector">
          <div class="db-selector-label">数据库连接</div>
          <el-radio-group v-model="form.DatabaseType" size="large">
            <el-radio-button v-for="opt in dbOptions" :key="opt" :value="opt">
              <el-icon v-if="opt === 'SQLite'" style="margin-right:4px"><Coin /></el-icon>
              <el-icon v-else style="margin-right:4px"><MagicStick /></el-icon>
              {{ opt === 'SQLite' ? 'SQLite (免安装)' : 'MySQL' }}
            </el-radio-button>
          </el-radio-group>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" size="large" :loading="loading" @click="handleLogin" style="width:100%">登 录</el-button>
        </el-form-item>
      </el-form>
      <div class="login-footer">
        <span>默认账号: admin / admin123</span>
        <span v-if="dbOptions.length === 1" class="db-hint">当前数据库: {{ dbOptions[0] }}</span>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, type FormInstance } from 'element-plus'
import { login, getDatabaseOptions } from '../api'
import { useAuthStore } from '../stores/auth'

const router = useRouter()
const auth = useAuthStore()
const formRef = ref<FormInstance>()
const loading = ref(false)
const dbOptions = ref<string[]>([])

const form = reactive({ Username: '', Password: '', DatabaseType: 'SQLite' })
const rules = {
  Username: [{ required: true, message: '请输入用户名', trigger: 'blur' }],
  Password: [{ required: true, message: '请输入密码', trigger: 'blur' }]
}

async function loadDbOptions() {
  try {
    const res: any = await getDatabaseOptions()
    if (res && res.enabledTypes) {
      dbOptions.value = res.enabledTypes
      form.DatabaseType = res.defaultType || res.enabledTypes[0]
    }
  } catch {
    // Fallback: default to SQLite only
    dbOptions.value = ['SQLite']
    form.DatabaseType = 'SQLite'
  }
}

async function handleLogin() {
  await formRef.value?.validate()
  loading.value = true
  try {
    const payload: any = { Username: form.Username, Password: form.Password }
    // Always include DatabaseType and set it in store BEFORE the request,
    // so the X-Database-Type header is sent with the login request.
    const selectedDb = dbOptions.value.length > 1 ? form.DatabaseType : (dbOptions.value[0] || 'SQLite')
    payload.DatabaseType = selectedDb
    auth.setDatabaseType(selectedDb)

    const res: any = await login(payload)
    if (res.success) {
      auth.setAuth(res.data)
      ElMessage.success('登录成功')
      router.push('/')
    }
  } finally {
    loading.value = false
  }
}

onMounted(() => { loadDbOptions() })
</script>

<style scoped>
.login-page {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}
.login-card {
  width: 420px;
  padding: 40px;
  background: #fff;
  border-radius: 12px;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
}
.login-header {
  text-align: center;
  margin-bottom: 32px;
}
.login-header h1 {
  margin-top: 12px;
  font-size: 24px;
  color: #303133;
}
.subtitle {
  color: #909399;
  font-size: 14px;
  margin-top: 4px;
}
.db-selector {
  margin-bottom: 20px;
}
.db-selector-label {
  font-size: 13px;
  color: #606266;
  margin-bottom: 8px;
  width: 100%;
}
.db-selector :deep(.el-radio-group) {
  width: 100%;
  display: flex;
}
.db-selector :deep(.el-radio-button) {
  flex: 1;
}
.db-selector :deep(.el-radio-button__inner) {
  width: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
}
.login-footer {
  text-align: center;
  color: #c0c4cc;
  font-size: 12px;
  margin-top: 16px;
  display: flex;
  flex-direction: column;
  gap: 4px;
}
.db-hint {
  color: #909399;
}
</style>
