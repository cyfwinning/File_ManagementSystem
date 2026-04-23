<template>
  <div class="spaces-page">
    <div class="page-header">
      <h2>知识空间</h2>
      <el-button type="primary" @click="showCreateDialog = true"><el-icon><Plus /></el-icon>创建空间</el-button>
    </div>
    <el-row :gutter="20">
      <el-col :span="6" v-for="space in spaces" :key="space.id">
        <el-card shadow="hover" class="space-card" @click="$router.push(`/spaces/${space.id}`)">
          <div class="space-icon">
            <el-icon :size="48" :color="typeColors[space.type] || '#409eff'"><FolderOpened /></el-icon>
          </div>
          <h3>{{ space.name }}</h3>
          <p class="space-desc">{{ space.description || '暂无描述' }}</p>
          <div class="space-meta">
            <el-tag size="small">{{ typeLabels[space.type] || space.type }}</el-tag>
            <span>{{ space.documentCount }}篇文档</span>
          </div>
        </el-card>
      </el-col>
    </el-row>

    <el-dialog v-model="showCreateDialog" title="创建知识空间" width="500">
      <el-form :model="createForm" label-width="80px">
        <el-form-item label="名称">
          <el-input v-model="createForm.Name" placeholder="空间名称" />
        </el-form-item>
        <el-form-item label="描述">
          <el-input v-model="createForm.Description" type="textarea" rows="3" />
        </el-form-item>
        <el-form-item label="类型">
          <el-select v-model="createForm.Type">
            <el-option :value="0" label="企业空间" />
            <el-option :value="1" label="部门空间" />
            <el-option :value="2" label="项目空间" />
            <el-option :value="3" label="个人空间" />
          </el-select>
        </el-form-item>
        <el-form-item label="可见性">
          <el-select v-model="createForm.Visibility">
            <el-option :value="0" label="公开" />
            <el-option :value="1" label="内部" />
            <el-option :value="2" label="加密" />
          </el-select>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showCreateDialog = false">取消</el-button>
        <el-button type="primary" @click="handleCreate" :loading="creating">创建</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { getSpaces, createSpace } from '../api'

const spaces = ref<any[]>([])
const showCreateDialog = ref(false)
const creating = ref(false)
const createForm = reactive({ Name: '', Description: '', Type: 0, Visibility: 1 })

const typeLabels: Record<string, string> = { Enterprise: '企业', Department: '部门', Project: '项目', Personal: '个人' }
const typeColors: Record<string, string> = { Enterprise: '#409eff', Department: '#67c23a', Project: '#e6a23c', Personal: '#909399' }

async function loadSpaces() {
  const res: any = await getSpaces()
  if (res.success) spaces.value = res.data
}

async function handleCreate() {
  creating.value = true
  try {
    const res: any = await createSpace(createForm)
    if (res.success) {
      ElMessage.success('创建成功')
      showCreateDialog.value = false
      await loadSpaces()
    }
  } finally { creating.value = false }
}

onMounted(loadSpaces)
</script>

<style scoped>
.page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px; }
.space-card { cursor: pointer; text-align: center; margin-bottom: 20px; transition: transform 0.2s; }
.space-card:hover { transform: translateY(-4px); }
.space-icon { margin-bottom: 12px; }
.space-card h3 { font-size: 16px; margin-bottom: 8px; }
.space-desc { color: #909399; font-size: 13px; margin-bottom: 12px; height: 36px; overflow: hidden; }
.space-meta { display: flex; justify-content: space-between; align-items: center; font-size: 12px; color: #909399; }
</style>
