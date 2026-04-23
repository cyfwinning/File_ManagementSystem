<template>
  <div class="system-page">
    <h2 style="margin-bottom:20px">系统配置</h2>

    <!-- Database Availability Config -->
    <el-card style="margin-bottom:20px">
      <template #header><span>数据库可用性管理</span></template>
      <el-alert type="info" :closable="false" style="margin-bottom:16px">
        控制登录页面可用的数据库类型。若仅启用一种数据库，登录页将不显示选择框；若两种都启用，则登录页显示数据库选择框。
      </el-alert>
      <el-form :model="dbAvailForm" label-width="140px">
        <el-form-item label="启用 SQLite">
          <el-switch v-model="dbAvailForm.enableSQLite" :disabled="!dbAvailForm.enableMySQL" />
          <span class="form-hint">免安装，适合小规模部署</span>
        </el-form-item>
        <el-form-item label="启用 MySQL">
          <el-switch v-model="dbAvailForm.enableMySQL" :disabled="!dbAvailForm.enableSQLite" />
          <span class="form-hint">适合生产环境和大规模数据</span>
        </el-form-item>
        <el-form-item label="默认数据库">
          <el-radio-group v-model="dbAvailForm.defaultType">
            <el-radio value="SQLite" :disabled="!dbAvailForm.enableSQLite">SQLite</el-radio>
            <el-radio value="MySQL" :disabled="!dbAvailForm.enableMySQL">MySQL</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-divider content-position="left">MySQL 连接配置</el-divider>
        <el-form-item label="服务器地址">
          <el-input v-model="mysqlParts.server" placeholder="localhost" style="width:280px" />
        </el-form-item>
        <el-form-item label="端口">
          <el-input-number v-model="mysqlParts.port" :min="1" :max="65535" />
        </el-form-item>
        <el-form-item label="数据库名">
          <el-input v-model="mysqlParts.database" placeholder="knowledgebase" style="width:280px" />
        </el-form-item>
        <el-form-item label="用户名">
          <el-input v-model="mysqlParts.username" style="width:280px" />
        </el-form-item>
        <el-form-item label="密码">
          <el-input v-model="mysqlParts.password" type="password" show-password style="width:280px" />
        </el-form-item>
        <el-form-item>
          <el-button type="warning" @click="handleTestMySql" :loading="testingMysql">测试 MySQL 连接</el-button>
          <el-button type="primary" @click="saveDbAvailConfig">保存配置</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <!-- System Configs -->
    <el-card>
      <template #header><span>参数配置</span></template>
      <el-table :data="configs" stripe>
        <el-table-column prop="configKey" label="配置项" width="280" />
        <el-table-column prop="configValue" label="值" min-width="200">
          <template #default="{ row }">
            <el-input v-model="row.configValue" size="small" @change="handleUpdateConfig(row)" />
          </template>
        </el-table-column>
        <el-table-column prop="group" label="分组" width="100" />
        <el-table-column prop="description" label="说明" width="200" />
      </el-table>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { getSystemConfigs, updateSystemConfig, getDatabaseConfig, configureDatabaseConfig, testMySqlConnection } from '../../api'

const configs = ref<any[]>([])
const testingMysql = ref(false)

const dbAvailForm = reactive({
  enableSQLite: true,
  enableMySQL: false,
  defaultType: 'SQLite'
})

const mysqlParts = reactive({
  server: 'localhost',
  port: 3306,
  database: 'knowledgebase',
  username: 'root',
  password: ''
})

function buildMySqlConnString() {
  return `Server=${mysqlParts.server};Port=${mysqlParts.port};Database=${mysqlParts.database};User=${mysqlParts.username};Password=${mysqlParts.password};`
}

async function loadConfigs() {
  const res: any = await getSystemConfigs()
  if (res.success) configs.value = res.data
}

async function loadDbConfig() {
  try {
    const res: any = await getDatabaseConfig()
    if (res) {
      dbAvailForm.enableSQLite = res.enableSQLite ?? true
      dbAvailForm.enableMySQL = res.enableMySQL ?? false
      dbAvailForm.defaultType = res.defaultType || 'SQLite'
      // Parse masked MySQL connection string for display
      if (res.mysqlConnectionString) {
        parseMySqlConnStr(res.mysqlConnectionString)
      }
    }
  } catch {
    // ignore
  }
}

function parseMySqlConnStr(connStr: string) {
  const parts = connStr.split(';').filter(Boolean)
  for (const part of parts) {
    const eq = part.indexOf('=')
    if (eq < 0) continue
    const key = part.substring(0, eq).trim().toLowerCase()
    const val = part.substring(eq + 1).trim()
    if (key === 'server') mysqlParts.server = val
    else if (key === 'port') mysqlParts.port = parseInt(val) || 3306
    else if (key === 'database') mysqlParts.database = val
    else if (key === 'user') mysqlParts.username = val
    // Don't populate password since it's masked
  }
}

async function handleUpdateConfig(row: any) {
  await updateSystemConfig(row.id, row.configValue)
  ElMessage.success('配置已更新')
}

async function handleTestMySql() {
  testingMysql.value = true
  try {
    const res: any = await testMySqlConnection({ connectionString: buildMySqlConnString() })
    if (res.success) {
      ElMessage.success(res.message)
    } else {
      ElMessage.error(res.message)
    }
  } catch {
    ElMessage.error('测试请求失败')
  } finally {
    testingMysql.value = false
  }
}

async function saveDbAvailConfig() {
  const data: any = {
    enableSQLite: dbAvailForm.enableSQLite,
    enableMySQL: dbAvailForm.enableMySQL,
    defaultType: dbAvailForm.defaultType,
    mySQLConnectionString: buildMySqlConnString()
  }
  try {
    const res: any = await configureDatabaseConfig(data)
    if (res.success) {
      ElMessage.success(res.message)
    }
  } catch {
    ElMessage.error('保存失败')
  }
}

onMounted(() => { loadConfigs(); loadDbConfig() })
</script>

<style scoped>
.form-hint {
  margin-left: 12px;
  color: #909399;
  font-size: 13px;
}
</style>
