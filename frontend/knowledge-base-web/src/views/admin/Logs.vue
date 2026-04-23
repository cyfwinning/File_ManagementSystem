<template>
  <div class="logs-page">
    <h2 style="margin-bottom:20px">操作日志</h2>
    <el-card>
      <el-table :data="logs" stripe>
        <el-table-column prop="id" label="ID" width="80" />
        <el-table-column prop="userId" label="用户ID" width="80" />
        <el-table-column prop="module" label="模块" width="100" />
        <el-table-column prop="action" label="操作" width="120" />
        <el-table-column prop="detail" label="详情" min-width="200" show-overflow-tooltip />
        <el-table-column prop="ipAddress" label="IP" width="140" />
        <el-table-column prop="createdAt" label="时间" width="180">
          <template #default="{ row }">{{ new Date(row.createdAt).toLocaleString() }}</template>
        </el-table-column>
      </el-table>
      <el-pagination
        v-if="total > pageSize"
        :current-page="page" :page-size="pageSize" :total="total"
        layout="total, prev, pager, next" style="margin-top:16px"
        @current-change="(p: number) => { page = p; loadLogs() }"
      />
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { getOperationLogs } from '../../api'

const logs = ref<any[]>([])
const page = ref(1)
const pageSize = 50
const total = ref(0)

async function loadLogs() {
  const res: any = await getOperationLogs({ page: page.value, pageSize })
  if (res) { logs.value = res.items || []; total.value = res.totalCount || 0 }
}

onMounted(loadLogs)
</script>
