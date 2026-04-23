<template>
  <div class="users-page">
    <div class="page-header">
      <h2>用户管理</h2>
      <el-button type="primary" @click="showDialog = true"><el-icon><Plus /></el-icon>新增用户</el-button>
    </div>
    <el-card>
      <el-input v-model="keyword" placeholder="搜索用户..." style="width:300px;margin-bottom:16px" clearable @change="loadUsers">
        <template #prefix><el-icon><Search /></el-icon></template>
      </el-input>
      <el-table :data="users" stripe>
        <el-table-column prop="username" label="用户名" width="120" />
        <el-table-column prop="displayName" label="姓名" width="120" />
        <el-table-column prop="role" label="角色" width="140">
          <template #default="{ row }">
            <el-tag :type="roleTypes[row.role] || 'info'" size="small">{{ roleLabels[row.role] || row.role }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="departmentName" label="部门" width="120" />
        <el-table-column prop="email" label="邮箱" min-width="180" />
        <el-table-column prop="isActive" label="状态" width="80">
          <template #default="{ row }"><el-tag :type="row.isActive ? 'success' : 'danger'" size="small">{{ row.isActive ? '启用' : '禁用' }}</el-tag></template>
        </el-table-column>
        <el-table-column label="操作" width="160" fixed="right">
          <template #default="{ row }">
            <el-button size="small" text type="primary" @click="editUser(row)">编辑</el-button>
            <el-button size="small" text type="danger" @click="handleDelete(row.id)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
      <el-pagination
        v-if="total > pageSize"
        :current-page="page" :page-size="pageSize" :total="total"
        layout="total, prev, pager, next" style="margin-top:16px"
        @current-change="(p: number) => { page = p; loadUsers() }"
      />
    </el-card>

    <el-dialog v-model="showDialog" :title="editingId ? '编辑用户' : '新增用户'" width="500">
      <el-form :model="form" label-width="80px">
        <el-form-item label="用户名" v-if="!editingId"><el-input v-model="form.Username" /></el-form-item>
        <el-form-item label="密码" v-if="!editingId"><el-input v-model="form.Password" type="password" /></el-form-item>
        <el-form-item label="姓名"><el-input v-model="form.DisplayName" /></el-form-item>
        <el-form-item label="邮箱"><el-input v-model="form.Email" /></el-form-item>
        <el-form-item label="手机"><el-input v-model="form.Phone" /></el-form-item>
        <el-form-item label="角色">
          <el-select v-model="form.Role" style="width:100%">
            <el-option :value="0" label="超级管理员" />
            <el-option :value="1" label="公司领导" />
            <el-option :value="2" label="部门领导" />
            <el-option :value="3" label="空间管理员" />
            <el-option :value="4" label="普通编辑" />
            <el-option :value="5" label="只读访客" />
          </el-select>
        </el-form-item>
        <el-form-item label="状态" v-if="editingId">
          <el-switch v-model="form.IsActive" active-text="启用" inactive-text="禁用" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showDialog = false">取消</el-button>
        <el-button type="primary" @click="handleSave">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { getUsers, createUser, updateUser, deleteUser } from '../../api'

const users = ref<any[]>([])
const keyword = ref('')
const page = ref(1)
const pageSize = 20
const total = ref(0)
const showDialog = ref(false)
const editingId = ref<number | null>(null)
const form = reactive({ Username: '', Password: '', DisplayName: '', Email: '', Phone: '', Role: 4, DepartmentId: null as number | null, IsActive: true })

const roleLabels: Record<string, string> = { SuperAdmin: '超级管理员', CompanyLeader: '公司领导', DepartmentLeader: '部门领导', SpaceAdmin: '空间管理员', Editor: '普通编辑', ReadOnly: '只读访客' }
const roleTypes: Record<string, string> = { SuperAdmin: 'danger', CompanyLeader: 'warning', DepartmentLeader: 'warning', SpaceAdmin: '', Editor: 'info', ReadOnly: 'info' }

async function loadUsers() {
  const res: any = await getUsers({ page: page.value, pageSize, keyword: keyword.value })
  if (res.success) { users.value = res.data.items; total.value = res.data.totalCount }
}

function editUser(row: any) {
  editingId.value = row.id
  form.DisplayName = row.displayName
  form.Email = row.email
  form.Phone = row.phone
  form.Role = ['SuperAdmin','CompanyLeader','DepartmentLeader','SpaceAdmin','Editor','ReadOnly'].indexOf(row.role)
  form.IsActive = row.isActive
  showDialog.value = true
}

async function handleSave() {
  if (editingId.value) {
    const res: any = await updateUser(editingId.value, { DisplayName: form.DisplayName, Email: form.Email, Phone: form.Phone, Role: form.Role, DepartmentId: form.DepartmentId, IsActive: form.IsActive })
    if (res.success) { ElMessage.success('更新成功'); showDialog.value = false; editingId.value = null; await loadUsers() }
  } else {
    const res: any = await createUser(form)
    if (res.success) { ElMessage.success('创建成功'); showDialog.value = false; await loadUsers() }
  }
}

async function handleDelete(id: number) {
  await ElMessageBox.confirm('确定删除此用户？', '警告', { type: 'warning' })
  const res: any = await deleteUser(id)
  if (res.success) { ElMessage.success('删除成功'); await loadUsers() }
}

onMounted(loadUsers)
</script>

<style scoped>
.page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px; }
</style>
