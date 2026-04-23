<template>
  <div class="exams-page">
    <div class="page-header">
      <h2>考试中心</h2>
      <el-button type="primary" @click="showCreateDialog = true" v-if="auth.isLeader"><el-icon><Plus /></el-icon>创建考试</el-button>
    </div>

    <el-row :gutter="20">
      <el-col :span="8" v-for="exam in exams" :key="exam.id">
        <el-card shadow="hover" class="exam-card" @click="$router.push(`/exams/${exam.id}`)">
          <h3>{{ exam.title }}</h3>
          <p class="exam-desc">{{ exam.description || '暂无描述' }}</p>
          <div class="exam-meta">
            <el-tag size="small">{{ exam.departmentName || '全部门' }}</el-tag>
            <span>{{ exam.questionCount }}题</span>
            <span>{{ exam.timeLimitMinutes }}分钟</span>
            <span>满分{{ exam.totalScore }}</span>
          </div>
          <div class="exam-time">
            <span v-if="exam.startTime">开始: {{ new Date(exam.startTime).toLocaleString() }}</span>
            <span v-if="exam.endTime">截止: {{ new Date(exam.endTime).toLocaleString() }}</span>
          </div>
        </el-card>
      </el-col>
    </el-row>
    <el-empty v-if="!exams.length" description="暂无考试" />

    <!-- Create Exam Dialog -->
    <el-dialog v-model="showCreateDialog" title="创建考试" width="700" top="5vh">
      <el-form :model="examForm" label-width="100px">
        <el-form-item label="考试标题"><el-input v-model="examForm.Title" /></el-form-item>
        <el-form-item label="描述"><el-input v-model="examForm.Description" type="textarea" rows="2" /></el-form-item>
        <el-row :gutter="20">
          <el-col :span="8"><el-form-item label="时间限制(分)"><el-input-number v-model="examForm.TimeLimitMinutes" :min="1" /></el-form-item></el-col>
          <el-col :span="8"><el-form-item label="及格分"><el-input-number v-model="examForm.PassScore" :min="0" /></el-form-item></el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12"><el-form-item label="开始时间"><el-date-picker v-model="examForm.StartTime" type="datetime" style="width:100%" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="截止时间"><el-date-picker v-model="examForm.EndTime" type="datetime" style="width:100%" /></el-form-item></el-col>
        </el-row>

        <el-divider>题目</el-divider>
        <div v-for="(q, idx) in examForm.Questions" :key="idx" class="question-block">
          <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:8px">
            <strong>第{{ idx + 1 }}题</strong>
            <el-button text type="danger" @click="examForm.Questions.splice(idx, 1)">删除</el-button>
          </div>
          <el-row :gutter="12">
            <el-col :span="8">
              <el-select v-model="q.QuestionType" placeholder="题型" style="width:100%">
                <el-option :value="0" label="单选题" />
                <el-option :value="1" label="多选题" />
                <el-option :value="2" label="填空题" />
              </el-select>
            </el-col>
            <el-col :span="8"><el-input-number v-model="q.Score" :min="1" placeholder="分值" /></el-col>
          </el-row>
          <el-input v-model="q.QuestionText" placeholder="题目内容" style="margin-top:8px" />
          <el-input v-model="q.Options" placeholder="选项(JSON: [&quot;A. xxx&quot;, &quot;B. xxx&quot;])" style="margin-top:8px" v-if="q.QuestionType !== 2" />
          <el-input v-model="q.CorrectAnswer" placeholder="正确答案" style="margin-top:8px" />
        </div>
        <el-button @click="addQuestion" style="margin-top:12px"><el-icon><Plus /></el-icon>添加题目</el-button>
      </el-form>
      <template #footer>
        <el-button @click="showCreateDialog = false">取消</el-button>
        <el-button type="primary" @click="handleCreateExam" :loading="creating">创建</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { useAuthStore } from '../stores/auth'
import { getExams, createExam } from '../api'

const auth = useAuthStore()
const exams = ref<any[]>([])
const showCreateDialog = ref(false)
const creating = ref(false)

const examForm = reactive({
  Title: '', Description: '', DepartmentId: null as number | null, TimeLimitMinutes: 60,
  StartTime: null as Date | null, EndTime: null as Date | null, PassScore: 60,
  Questions: [] as any[]
})

function addQuestion() {
  examForm.Questions.push({ QuestionType: 0, QuestionText: '', Options: '', CorrectAnswer: '', Score: 10, SortOrder: examForm.Questions.length })
}

async function loadExams() {
  const res: any = await getExams()
  if (res.success) exams.value = res.data.items
}

async function handleCreateExam() {
  if (!examForm.Title) { ElMessage.warning('请输入考试标题'); return }
  if (!examForm.Questions.length) { ElMessage.warning('请添加至少一道题目'); return }
  creating.value = true
  try {
    const res: any = await createExam(examForm)
    if (res.success) { ElMessage.success('创建成功'); showCreateDialog.value = false; await loadExams() }
  } finally { creating.value = false }
}

onMounted(loadExams)
</script>

<style scoped>
.page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px; }
.exam-card { cursor: pointer; margin-bottom: 20px; transition: transform 0.2s; }
.exam-card:hover { transform: translateY(-4px); }
.exam-card h3 { font-size: 16px; margin-bottom: 8px; }
.exam-desc { color: #909399; font-size: 13px; margin-bottom: 12px; }
.exam-meta { display: flex; gap: 12px; align-items: center; font-size: 12px; color: #909399; margin-bottom: 8px; }
.exam-time { font-size: 12px; color: #c0c4cc; }
.question-block { padding: 16px; margin-bottom: 12px; background: #fafafa; border-radius: 8px; border: 1px solid #ebeef5; }
</style>
