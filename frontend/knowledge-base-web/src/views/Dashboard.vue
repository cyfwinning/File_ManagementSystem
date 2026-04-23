<template>
  <div class="dashboard-page">
    <!-- Personal Stats Cards -->
    <div class="stats-section">
      <h3 class="section-title">
        <el-icon><DataAnalysis /></el-icon>
        <span>个人学习概览</span>
      </h3>
      <el-row :gutter="16">
        <el-col :xs="12" :sm="8" :md="4">
          <div class="stat-card card-blue">
            <div class="stat-icon"><el-icon :size="28"><Calendar /></el-icon></div>
            <div class="stat-body">
              <div class="stat-value">{{ personal.yearReadCount }}</div>
              <div class="stat-label">本年阅读数</div>
            </div>
          </div>
        </el-col>
        <el-col :xs="12" :sm="8" :md="4">
          <div class="stat-card card-cyan">
            <div class="stat-icon"><el-icon :size="28"><Timer /></el-icon></div>
            <div class="stat-body">
              <div class="stat-value">{{ formatMinutes(personal.yearReadSeconds) }}</div>
              <div class="stat-label">本年学习时长</div>
            </div>
          </div>
        </el-col>
        <el-col :xs="12" :sm="8" :md="4">
          <div class="stat-card card-green">
            <div class="stat-icon"><el-icon :size="28"><Document /></el-icon></div>
            <div class="stat-body">
              <div class="stat-value">{{ personal.weekReadCount }}</div>
              <div class="stat-label">本周阅读数</div>
            </div>
          </div>
        </el-col>
        <el-col :xs="12" :sm="8" :md="4">
          <div class="stat-card card-orange">
            <div class="stat-icon"><el-icon :size="28"><Clock /></el-icon></div>
            <div class="stat-body">
              <div class="stat-value">{{ formatMinutes(personal.weekReadSeconds) }}</div>
              <div class="stat-label">本周学习时长</div>
            </div>
          </div>
        </el-col>
        <el-col :xs="12" :sm="8" :md="4">
          <div class="stat-card card-purple">
            <div class="stat-icon"><el-icon :size="28"><TrendCharts /></el-icon></div>
            <div class="stat-body">
              <div class="stat-value">{{ personal.last7DaysReadCount }}</div>
              <div class="stat-label">近7日阅读数</div>
            </div>
          </div>
        </el-col>
        <el-col :xs="12" :sm="8" :md="4">
          <div class="stat-card card-red">
            <div class="stat-icon"><el-icon :size="28"><Stopwatch /></el-icon></div>
            <div class="stat-body">
              <div class="stat-value">{{ formatMinutes(personal.last7DaysReadSeconds) }}</div>
              <div class="stat-label">近7日学习时长</div>
            </div>
          </div>
        </el-col>
      </el-row>
    </div>

    <!-- Trend Chart -->
    <el-card class="chart-card" shadow="hover">
      <template #header>
        <div class="chart-header">
          <div class="chart-title">
            <el-icon><TrendCharts /></el-icon>
            <span>阅读趋势</span>
          </div>
          <el-radio-group v-model="trendPeriod" size="small" @change="loadTrend">
            <el-radio-button value="year">按年</el-radio-button>
            <el-radio-button value="month">按月</el-radio-button>
            <el-radio-button value="week">按周</el-radio-button>
            <el-radio-button value="7day">近7日</el-radio-button>
          </el-radio-group>
        </div>
      </template>
      <div ref="trendChartRef" class="chart-container"></div>
    </el-card>

    <!-- Department Stats (Leaders Only) -->
    <template v-if="auth.isLeader">
      <el-card class="dept-card" shadow="hover">
        <template #header>
          <div class="chart-header">
            <div class="chart-title">
              <el-icon><OfficeBuilding /></el-icon>
              <span>{{ deptStats.departmentName || '部门' }} - 成员学习统计</span>
            </div>
            <el-tag type="info">共 {{ deptStats.totalMembers }} 人</el-tag>
          </div>
        </template>
        <!-- Department summary -->
        <el-row :gutter="16" class="dept-summary">
          <el-col :span="8">
            <el-statistic title="部门总阅读数" :value="deptStats.totalReadCount" />
          </el-col>
          <el-col :span="8">
            <el-statistic title="部门总学习时长(分钟)" :value="Math.round(deptStats.totalReadSeconds / 60)" />
          </el-col>
          <el-col :span="8">
            <el-statistic title="已完成文档数" :value="deptStats.totalCompletedCount" />
          </el-col>
        </el-row>

        <!-- Department members ranking chart -->
        <div ref="deptChartRef" class="chart-container" style="margin-top: 20px;"></div>

        <!-- Members Table -->
        <el-table :data="deptStats.members" stripe style="width: 100%; margin-top: 20px;" :default-sort="{ prop: 'readSeconds', order: 'descending' }">
          <el-table-column type="index" label="#" width="50" />
          <el-table-column prop="displayName" label="姓名" width="120" />
          <el-table-column prop="readCount" label="阅读文档数" sortable width="130" />
          <el-table-column label="学习时长" sortable :sort-by="(row: any) => row.readSeconds" width="130">
            <template #default="{ row }">{{ formatMinutes(row.readSeconds) }}</template>
          </el-table-column>
          <el-table-column prop="completedCount" label="已完成" sortable width="100" />
          <el-table-column label="最后学习时间" width="180">
            <template #default="{ row }">{{ row.lastAccessAt ? new Date(row.lastAccessAt).toLocaleString() : '-' }}</template>
          </el-table-column>
          <el-table-column label="操作" width="100">
            <template #default="{ row }">
              <el-button type="primary" link size="small" @click="viewUserDetail(row)">详情</el-button>
            </template>
          </el-table-column>
        </el-table>
      </el-card>
    </template>

    <!-- User Detail Dialog -->
    <el-dialog v-model="userDetailVisible" :title="`${userDetailName} - 学习统计`" width="600px">
      <el-row :gutter="16" v-if="userDetail">
        <el-col :span="8"><el-statistic title="本年阅读数" :value="userDetail.yearReadCount" /></el-col>
        <el-col :span="8"><el-statistic title="本年时长(分钟)" :value="Math.round(userDetail.yearReadSeconds / 60)" /></el-col>
        <el-col :span="8"><el-statistic title="本周阅读数" :value="userDetail.weekReadCount" /></el-col>
        <el-col :span="8" style="margin-top:16px"><el-statistic title="本周时长(分钟)" :value="Math.round(userDetail.weekReadSeconds / 60)" /></el-col>
        <el-col :span="8" style="margin-top:16px"><el-statistic title="近7日阅读数" :value="userDetail.last7DaysReadCount" /></el-col>
        <el-col :span="8" style="margin-top:16px"><el-statistic title="近7日时长(分钟)" :value="Math.round(userDetail.last7DaysReadSeconds / 60)" /></el-col>
      </el-row>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, nextTick, watch } from 'vue'
import { useAuthStore } from '../stores/auth'
import { getDashboardPersonal, getDashboardTrend, getDashboardDepartment, getDashboardUserStats } from '../api'
import * as echarts from 'echarts'

const auth = useAuthStore()

const personal = ref({
  yearReadCount: 0, yearReadSeconds: 0,
  weekReadCount: 0, weekReadSeconds: 0,
  last7DaysReadCount: 0, last7DaysReadSeconds: 0,
  totalReadCount: 0, totalReadSeconds: 0
})

const trendPeriod = ref('7day')
const trendChartRef = ref<HTMLElement>()
let trendChart: echarts.ECharts | null = null

const deptStats = ref<any>({
  departmentName: '', totalMembers: 0,
  totalReadCount: 0, totalReadSeconds: 0, totalCompletedCount: 0,
  members: []
})
const deptChartRef = ref<HTMLElement>()
let deptChart: echarts.ECharts | null = null

const userDetailVisible = ref(false)
const userDetailName = ref('')
const userDetail = ref<any>(null)

function formatMinutes(seconds: number) {
  if (seconds < 60) return seconds + '秒'
  const m = Math.round(seconds / 60)
  if (m < 60) return m + '分钟'
  const h = Math.floor(m / 60)
  const rm = m % 60
  return h + '小时' + (rm > 0 ? rm + '分' : '')
}

async function loadPersonal() {
  try {
    const res: any = await getDashboardPersonal()
    if (res.success && res.data) personal.value = res.data
  } catch {}
}

async function loadTrend() {
  try {
    const res: any = await getDashboardTrend(trendPeriod.value)
    if (res.success && res.data) {
      await nextTick()
      renderTrendChart(res.data)
    }
  } catch {}
}

function renderTrendChart(data: any) {
  if (!trendChartRef.value) return
  if (!trendChart) {
    trendChart = echarts.init(trendChartRef.value)
  }
  const labels = data.points.map((p: any) => p.label)
  const counts = data.points.map((p: any) => p.readCount)
  const seconds = data.points.map((p: any) => Math.round(p.readSeconds / 60))

  trendChart.setOption({
    tooltip: {
      trigger: 'axis',
      axisPointer: { type: 'cross' }
    },
    legend: { data: ['阅读数量', '学习时长(分钟)'], bottom: 0 },
    grid: { left: 60, right: 60, top: 40, bottom: 40 },
    xAxis: {
      type: 'category',
      data: labels,
      axisLabel: { rotate: labels.length > 12 ? 45 : 0 }
    },
    yAxis: [
      { type: 'value', name: '阅读数量', position: 'left', minInterval: 1 },
      { type: 'value', name: '时长(分钟)', position: 'right' }
    ],
    series: [
      {
        name: '阅读数量',
        type: 'bar',
        yAxisIndex: 0,
        data: counts,
        itemStyle: {
          borderRadius: [4, 4, 0, 0],
          color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
            { offset: 0, color: '#409eff' },
            { offset: 1, color: '#79bbff' }
          ])
        }
      },
      {
        name: '学习时长(分钟)',
        type: 'line',
        yAxisIndex: 1,
        data: seconds,
        smooth: true,
        lineStyle: { width: 3, color: '#67c23a' },
        areaStyle: {
          color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
            { offset: 0, color: 'rgba(103, 194, 58, 0.3)' },
            { offset: 1, color: 'rgba(103, 194, 58, 0.05)' }
          ])
        },
        itemStyle: { color: '#67c23a' }
      }
    ]
  })
}

async function loadDeptStats() {
  if (!auth.isLeader) return
  try {
    const res: any = await getDashboardDepartment()
    if (res.success && res.data) {
      deptStats.value = res.data
      await nextTick()
      renderDeptChart(res.data)
    }
  } catch {}
}

function renderDeptChart(data: any) {
  if (!deptChartRef.value || !data.members?.length) return
  if (!deptChart) {
    deptChart = echarts.init(deptChartRef.value)
  }
  const sorted = [...data.members].sort((a: any, b: any) => b.readSeconds - a.readSeconds).slice(0, 15)
  const names = sorted.map((m: any) => m.displayName)
  const minutes = sorted.map((m: any) => Math.round(m.readSeconds / 60))
  const counts = sorted.map((m: any) => m.readCount)

  deptChart.setOption({
    tooltip: { trigger: 'axis' },
    legend: { data: ['学习时长(分钟)', '阅读数量'], bottom: 0 },
    grid: { left: 80, right: 40, top: 20, bottom: 40 },
    yAxis: { type: 'category', data: names.reverse(), axisLabel: { width: 60, overflow: 'truncate' } },
    xAxis: { type: 'value' },
    series: [
      {
        name: '学习时长(分钟)',
        type: 'bar',
        data: minutes.reverse(),
        itemStyle: {
          borderRadius: [0, 4, 4, 0],
          color: new echarts.graphic.LinearGradient(0, 0, 1, 0, [
            { offset: 0, color: '#409eff' },
            { offset: 1, color: '#79bbff' }
          ])
        }
      },
      {
        name: '阅读数量',
        type: 'bar',
        data: counts.reverse(),
        itemStyle: {
          borderRadius: [0, 4, 4, 0],
          color: new echarts.graphic.LinearGradient(0, 0, 1, 0, [
            { offset: 0, color: '#67c23a' },
            { offset: 1, color: '#95d475' }
          ])
        }
      }
    ]
  })
}

async function viewUserDetail(row: any) {
  userDetailName.value = row.displayName
  userDetailVisible.value = true
  try {
    const res: any = await getDashboardUserStats(row.userId)
    if (res.success && res.data) userDetail.value = res.data
  } catch {}
}

function handleResize() {
  trendChart?.resize()
  deptChart?.resize()
}

onMounted(async () => {
  await Promise.all([loadPersonal(), loadTrend(), loadDeptStats()])
  window.addEventListener('resize', handleResize)
})

onUnmounted(() => {
  window.removeEventListener('resize', handleResize)
  trendChart?.dispose()
  deptChart?.dispose()
})
</script>

<style scoped>
.dashboard-page {
  padding: 0;
}

.stats-section {
  margin-bottom: 20px;
}

.section-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 16px;
  font-weight: 600;
  color: #303133;
  margin-bottom: 16px;
}

.stat-card {
  display: flex;
  align-items: center;
  padding: 20px 16px;
  border-radius: 12px;
  color: #fff;
  margin-bottom: 12px;
  transition: transform 0.2s, box-shadow 0.2s;
  cursor: default;
}
.stat-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 8px 24px rgba(0,0,0,0.15);
}
.stat-icon {
  width: 48px;
  height: 48px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(255,255,255,0.2);
  border-radius: 12px;
  margin-right: 14px;
  flex-shrink: 0;
}
.stat-body { flex: 1; }
.stat-value { font-size: 22px; font-weight: 700; line-height: 1.2; }
.stat-label { font-size: 12px; opacity: 0.85; margin-top: 4px; }

.card-blue { background: linear-gradient(135deg, #409eff, #337ecc); }
.card-cyan { background: linear-gradient(135deg, #00bcd4, #0097a7); }
.card-green { background: linear-gradient(135deg, #67c23a, #529b2e); }
.card-orange { background: linear-gradient(135deg, #e6a23c, #c48a2c); }
.card-purple { background: linear-gradient(135deg, #9c27b0, #7b1fa2); }
.card-red { background: linear-gradient(135deg, #f56c6c, #c45656); }

.chart-card, .dept-card {
  margin-bottom: 20px;
  border-radius: 12px;
}

.chart-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.chart-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 16px;
  font-weight: 600;
}

.chart-container {
  width: 100%;
  height: 360px;
}

.dept-summary {
  padding: 12px 0;
  text-align: center;
}
</style>
