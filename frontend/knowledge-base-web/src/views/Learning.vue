<template>
  <div class="learning-page">
    <h2 style="margin-bottom:20px">学习中心</h2>

    <!-- Stats -->
    <el-row :gutter="20" style="margin-bottom:20px">
      <el-col :span="6">
        <el-card shadow="hover"><el-statistic title="学习文档总数" :value="stats.totalDocuments" /></el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="hover"><el-statistic title="已完成" :value="stats.completedDocuments" /></el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="hover"><el-statistic title="学习中" :value="stats.inProgressDocuments" /></el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="hover"><el-statistic title="累计学习(分钟)" :value="Math.round(stats.totalSeconds / 60)" /></el-card>
      </el-col>
    </el-row>

    <!-- Learning Records -->
    <el-card>
      <template #header><span>学习记录</span></template>
      <el-table :data="records" stripe>
        <el-table-column prop="documentTitle" label="文档标题" min-width="200">
          <template #default="{ row }">
            <el-link type="primary" @click="$router.push(`/documents/${row.documentId}`)">{{ row.documentTitle }}</el-link>
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="100">
          <template #default="{ row }">
            <el-tag :type="row.status === 'Completed' ? 'success' : row.status === 'InProgress' ? 'warning' : 'info'" size="small">
              {{ { Completed: '已完成', InProgress: '学习中', NotStarted: '未开始' }[row.status as string] || row.status }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="progressPercent" label="进度" width="150">
          <template #default="{ row }">
            <el-progress :percentage="row.progressPercent" :status="row.progressPercent >= 100 ? 'success' : ''" :stroke-width="8" />
          </template>
        </el-table-column>
        <el-table-column prop="totalSeconds" label="学习时长" width="100">
          <template #default="{ row }">{{ Math.round(row.totalSeconds / 60) }}分钟</template>
        </el-table-column>
        <el-table-column prop="lastAccessAt" label="最近学习" width="160">
          <template #default="{ row }">{{ row.lastAccessAt ? new Date(row.lastAccessAt).toLocaleString() : '-' }}</template>
        </el-table-column>
      </el-table>
      <el-pagination
        v-if="total > pageSize"
        :current-page="page"
        :page-size="pageSize"
        :total="total"
        layout="total, prev, pager, next"
        style="margin-top:16px;justify-content:flex-end"
        @current-change="(p: number) => { page = p; loadRecords() }"
      />
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { getLearningStats, getLearningRecords } from '../api'

const stats = ref({ totalDocuments: 0, completedDocuments: 0, inProgressDocuments: 0, notStartedDocuments: 0, totalSeconds: 0 })
const records = ref<any[]>([])
const page = ref(1)
const pageSize = 20
const total = ref(0)

async function loadStats() {
  const res: any = await getLearningStats()
  if (res.success) stats.value = res.data
}

async function loadRecords() {
  const res: any = await getLearningRecords({ page: page.value, pageSize })
  if (res.success) { records.value = res.data.items; total.value = res.data.totalCount }
}

onMounted(() => { loadStats(); loadRecords() })
</script>
