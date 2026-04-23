<template>
  <div class="home-page">
    <!-- Recommendations Section -->
    <el-card class="section-card" v-if="recommendations.length">
      <template #header>
        <div class="section-header">
          <el-icon color="#e6a23c"><Star /></el-icon>
          <span>领导推荐必学文档</span>
          <el-tag type="danger" size="small">{{ recommendations.length }}篇待学</el-tag>
        </div>
      </template>
      <el-table :data="recommendations" stripe>
        <el-table-column prop="documentTitle" label="文档标题" min-width="200">
          <template #default="{ row }">
            <el-link type="primary" @click="$router.push(`/documents/${row.documentId}`)">{{ row.documentTitle }}</el-link>
          </template>
        </el-table-column>
        <el-table-column prop="recommenderName" label="推荐人" width="120" />
        <el-table-column prop="urgency" label="紧急程度" width="100">
          <template #default="{ row }">
            <el-tag :type="row.urgency === 'Urgent' ? 'danger' : row.urgency === 'High' ? 'warning' : 'info'" size="small">
              {{ { Urgent: '紧急', High: '高', Medium: '中', Low: '低' }[row.urgency as string] || row.urgency }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="isMandatory" label="类型" width="80">
          <template #default="{ row }">
            <el-tag :type="row.isMandatory ? 'danger' : 'info'" size="small">{{ row.isMandatory ? '必读' : '推荐' }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="deadline" label="截止日期" width="120">
          <template #default="{ row }">{{ row.deadline ? new Date(row.deadline).toLocaleDateString() : '-' }}</template>
        </el-table-column>
      </el-table>
    </el-card>

    <!-- Stats Cards -->
    <el-row :gutter="20" class="stats-row">
      <el-col :span="6">
        <el-card shadow="hover" class="stat-card">
          <el-statistic title="学习文档总数" :value="stats.totalDocuments" />
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="hover" class="stat-card">
          <el-statistic title="已完成" :value="stats.completedDocuments">
            <template #suffix><span style="font-size:14px;color:#67c23a">篇</span></template>
          </el-statistic>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="hover" class="stat-card">
          <el-statistic title="学习中" :value="stats.inProgressDocuments">
            <template #suffix><span style="font-size:14px;color:#e6a23c">篇</span></template>
          </el-statistic>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="hover" class="stat-card">
          <el-statistic title="累计学习时长" :value="Math.round(stats.totalSeconds / 60)">
            <template #suffix><span style="font-size:14px">分钟</span></template>
          </el-statistic>
        </el-card>
      </el-col>
    </el-row>

    <!-- Quick Actions -->
    <el-row :gutter="20" class="quick-actions">
      <el-col :span="8">
        <el-card shadow="hover" class="action-card" @click="$router.push('/spaces')">
          <el-icon :size="40" color="#409eff"><FolderOpened /></el-icon>
          <h3>知识空间</h3>
          <p>浏览和管理知识库</p>
        </el-card>
      </el-col>
      <el-col :span="8">
        <el-card shadow="hover" class="action-card" @click="$router.push('/documents/new')">
          <el-icon :size="40" color="#67c23a"><DocumentAdd /></el-icon>
          <h3>新建文档</h3>
          <p>创建新的知识文档</p>
        </el-card>
      </el-col>
      <el-col :span="8">
        <el-card shadow="hover" class="action-card" @click="$router.push('/learning')">
          <el-icon :size="40" color="#e6a23c"><TrophyBase /></el-icon>
          <h3>学习中心</h3>
          <p>查看学习进度和记录</p>
        </el-card>
      </el-col>
    </el-row>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { getMyRecommendations, getLearningStats } from '../api'

const recommendations = ref<any[]>([])
const stats = ref({ totalDocuments: 0, completedDocuments: 0, inProgressDocuments: 0, notStartedDocuments: 0, totalSeconds: 0 })

onMounted(async () => {
  try {
    const [recRes, statsRes] = await Promise.all([getMyRecommendations(), getLearningStats()]) as any[]
    if (recRes.success) recommendations.value = recRes.data
    if (statsRes.success) stats.value = statsRes.data
  } catch {}
})
</script>

<style scoped>
.home-page { padding: 0; }
.section-card { margin-bottom: 20px; }
.section-header { display: flex; align-items: center; gap: 8px; font-size: 16px; font-weight: 600; }
.stats-row { margin-bottom: 20px; }
.stat-card { text-align: center; }
.quick-actions { margin-bottom: 20px; }
.action-card { text-align: center; cursor: pointer; transition: transform 0.2s; padding: 20px 0; }
.action-card:hover { transform: translateY(-4px); }
.action-card h3 { margin: 12px 0 4px; color: #303133; }
.action-card p { color: #909399; font-size: 13px; }
</style>
