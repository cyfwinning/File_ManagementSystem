<template>
  <div class="dept-page">
    <div class="page-header">
      <h2>部门管理</h2>
      <el-button type="primary" @click="openCreateDialog(null)"><el-icon><Plus /></el-icon>新增根部门</el-button>
    </div>
    <el-card>
      <el-tree :data="departments" :props="{ label: 'name', children: 'children' }" node-key="id" default-expand-all>
        <template #default="{ node, data }">
          <span class="dept-node">
            <span class="dept-name"><el-icon><OfficeBuilding /></el-icon> {{ node.label }}</span>
            <span class="dept-actions">
              <el-button text type="primary" size="small" @click.stop="openCreateDialog(data)">
                <el-icon><Plus /></el-icon>新增下级
              </el-button>
              <el-button text type="warning" size="small" @click.stop="openEditDialog(data)">
                <el-icon><Edit /></el-icon>编辑
              </el-button>
              <el-button text type="danger" size="small" @click.stop="handleDelete(data.id)">
                <el-icon><Delete /></el-icon>删除
              </el-button>
            </span>
          </span>
        </template>
      </el-tree>
    </el-card>

    <!-- Create Dialog -->
    <el-dialog v-model="showCreateDialog" :title="createParent ? `新增「${createParent.name}」的下级部门` : '新增根部门'" width="400">
      <el-form :model="createForm" label-width="80px">
        <el-form-item label="名称"><el-input v-model="createForm.Name" placeholder="请输入部门名称" /></el-form-item>
        <el-form-item label="描述"><el-input v-model="createForm.Description" placeholder="可选" /></el-form-item>
        <el-form-item label="上级部门">
          <el-tree-select v-model="createForm.ParentId" :data="departments" :props="{ label: 'name', value: 'id', children: 'children' }" clearable check-strictly style="width:100%" placeholder="无（根部门）" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showCreateDialog = false">取消</el-button>
        <el-button type="primary" @click="handleCreate">创建</el-button>
      </template>
    </el-dialog>

    <!-- Edit Dialog -->
    <el-dialog v-model="showEditDialog" title="编辑部门" width="400">
      <el-form :model="editForm" label-width="80px">
        <el-form-item label="名称"><el-input v-model="editForm.Name" placeholder="请输入部门名称" /></el-form-item>
        <el-form-item label="描述"><el-input v-model="editForm.Description" placeholder="可选" /></el-form-item>
        <el-form-item label="排序"><el-input-number v-model="editForm.SortOrder" :min="0" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showEditDialog = false">取消</el-button>
        <el-button type="primary" @click="handleUpdate">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { getDepartmentTree, createDepartment, updateDepartment, deleteDepartment } from '../../api'

const departments = ref<any[]>([])

// Create
const showCreateDialog = ref(false)
const createParent = ref<any>(null)
const createForm = reactive({ Name: '', Description: '', ParentId: null as number | null, SortOrder: 0 })

// Edit
const showEditDialog = ref(false)
const editingId = ref<number>(0)
const editForm = reactive({ Name: '', Description: '', SortOrder: 0 })

async function loadDepts() {
  const res: any = await getDepartmentTree()
  if (res.success) departments.value = res.data
}

function openCreateDialog(parent: any) {
  createParent.value = parent
  createForm.Name = ''
  createForm.Description = ''
  createForm.ParentId = parent ? parent.id : null
  createForm.SortOrder = 0
  showCreateDialog.value = true
}

function openEditDialog(data: any) {
  editingId.value = data.id
  editForm.Name = data.name
  editForm.Description = data.description || ''
  editForm.SortOrder = data.sortOrder || 0
  showEditDialog.value = true
}

async function handleCreate() {
  if (!createForm.Name.trim()) {
    ElMessage.warning('请输入部门名称')
    return
  }
  const res: any = await createDepartment(createForm)
  if (res.success) {
    ElMessage.success('创建成功')
    showCreateDialog.value = false
    await loadDepts()
  }
}

async function handleUpdate() {
  if (!editForm.Name.trim()) {
    ElMessage.warning('请输入部门名称')
    return
  }
  const res: any = await updateDepartment(editingId.value, editForm)
  if (res.success) {
    ElMessage.success('更新成功')
    showEditDialog.value = false
    await loadDepts()
  }
}

async function handleDelete(id: number) {
  await ElMessageBox.confirm('确定删除此部门？', '警告', { type: 'warning' })
  const res: any = await deleteDepartment(id)
  if (res.success) { ElMessage.success('删除成功'); await loadDepts() }
}

onMounted(loadDepts)
</script>

<style scoped>
.page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px; }
.dept-node { display: flex; align-items: center; gap: 8px; flex: 1; justify-content: space-between; }
.dept-name { display: flex; align-items: center; gap: 4px; }
.dept-actions { display: flex; gap: 2px; }
</style>
